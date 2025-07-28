using DocumentProcessing.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace DocumentProcessing.API.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public AuthenticationMiddleware(RequestDelegate next, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _next = next;
            _serviceProvider = serviceProvider;

            var jwtSettings = configuration.GetSection("JwtSettings");
            var keyValue = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing in configuration.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));

            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for certain paths
            var path = context.Request.Path.Value?.ToLower();
            if (ShouldSkipAuthentication(path ?? string.Empty))
            {
                await _next(context);
                return;
            }

            await AuthenticateFromCookies(context);
            await _next(context);
        }

        private bool ShouldSkipAuthentication(string path)
        {
            var skipPaths = new[]
            {
                "/api/client/Auth/login",
                "/api/client/Auth/register",
                "/api/client/Auth/forgot-password",
                "/api/client/Auth/reset-password",
                "/api/client/Auth/confirm-email",
                "/api/client/Auth/resend-email-confirmation",
                "/api/client/health",
                "/swagger",
                "/favicon.ico"
            };

            return skipPaths.Any(skipPath => path?.StartsWith(skipPath) == true);
        }

        private async Task AuthenticateFromCookies(HttpContext context)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

                var (accessToken, refreshToken) = tokenService.GetTokensFromCookies();

                if (string.IsNullOrEmpty(accessToken))
                    return;

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(accessToken, _tokenValidationParameters, out SecurityToken validatedToken);

                context.User = principal;
            }
            catch (SecurityTokenExpiredException)
            {
                // Token is expired, try to refresh it
                await TryRefreshToken(context);
            }
            catch (Exception)
            {
                // Invalid token, clear cookies
                using var scope = _serviceProvider.CreateScope();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                tokenService.ClearTokenCookies();
            }
        }

        private async Task TryRefreshToken(HttpContext context)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

                var (accessToken, refreshToken) = tokenService.GetTokensFromCookies();

                if (string.IsNullOrEmpty(refreshToken))
                {
                    tokenService.ClearTokenCookies();
                    return;
                }

                var refreshTokenDto = new Application.DTOs.Auth.RefreshTokenDto
                {
                    AccessToken = accessToken ?? string.Empty,
                    RefreshToken = refreshToken
                };

                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = context.Request.Headers["User-Agent"].ToString();

                var result = await authService.RefreshTokenAsync(refreshTokenDto, ipAddress, userAgent);

                if (result.IsSuccess)
                {
                    // Tokens have been updated in cookies by the service
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var newAccessToken = tokenService.GetTokensFromCookies().AccessToken;

                    if (!string.IsNullOrEmpty(newAccessToken))
                    {
                        var principal = tokenHandler.ValidateToken(newAccessToken, _tokenValidationParameters, out SecurityToken validatedToken);
                        context.User = principal;
                    }
                }
                else
                {
                    tokenService.ClearTokenCookies();
                }
            }
            catch (Exception)
            {
                using var scope = _serviceProvider.CreateScope();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                tokenService.ClearTokenCookies();
            }
        }
    }
}
