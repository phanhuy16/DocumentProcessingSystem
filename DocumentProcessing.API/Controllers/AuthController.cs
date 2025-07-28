using DocumentProcessing.API.Helpers;
using DocumentProcessing.Application.DTOs.Auth;
using DocumentProcessing.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentProcessing.API.Controllers
{
    [ApiController]
    [Route("api/client/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;
        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(registerDto);

            if (result.IsSuccess)
            {
                // Return user info without tokens (tokens are in cookies)
                var response = new CookieAuthResponseDto
                {
                    UserId = result.Data.UserId,
                    Email = result.Data.Email,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName,
                    FullName = result.Data.FullName,
                    Role = result.Data.Role,
                    EmailConfirmed = result.Data.EmailConfirmed
                };

                return Ok(response);
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = AuthHelper.GetIpAddress(HttpContext);
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.LoginAsync(loginDto, ipAddress, userAgent);

            if (result.IsSuccess)
            {
                // Return user info without tokens (tokens are in cookies)
                var response = new CookieAuthResponseDto
                {
                    UserId = result.Data.UserId,
                    Email = result.Data.Email,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName,
                    FullName = result.Data.FullName,
                    Role = result.Data.Role,
                    EmailConfirmed = result.Data.EmailConfirmed
                };

                return Ok(response);
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            // RefreshTokenDto is optional since tokens can come from cookies
            refreshTokenDto ??= new RefreshTokenDto();

            var ipAddress = AuthHelper.GetIpAddress(HttpContext);
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.RefreshTokenAsync(refreshTokenDto, ipAddress, userAgent);

            if (result.IsSuccess)
            {
                var response = new CookieAuthResponseDto
                {
                    UserId = result.Data.UserId,
                    Email = result.Data.Email,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName,
                    FullName = result.Data.FullName,
                    Role = result.Data.Role,
                    EmailConfirmed = result.Data.EmailConfirmed
                };

                return Ok(response);
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Get session ID from somewhere if needed (could be from claims or cookies)
            var sessionId = AuthHelper.GetSessionId(HttpContext);
            var result = await _authService.LogoutAsync(sessionId);

            if (result.IsSuccess)
                return Ok(new { message = "Logged out successfully" });

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = AuthHelper.GetCurrentUserId(User);
            var result = await _authService.LogoutAllAsync(userId);

            if (result.IsSuccess)
                return Ok(new { message = "Logged out from all devices successfully" });

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = AuthHelper.GetCurrentUserId(User);
            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);

            if (result.IsSuccess)
                return Ok(new { message = "Password changed successfully" });

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);

            if (result.IsSuccess)
                return Ok(new { message = "Password reset email sent successfully" });

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(resetPasswordDto);

            if (result.IsSuccess)
                return Ok(new { message = "Password reset successfully" });

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Email and token are required" });

            var result = await _authService.ConfirmEmailAsync(email, token);

            if (result.IsSuccess)
                return Ok(new { message = "Email confirmed successfully" });

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("resend-email-confirmation")]
        public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationDto resendDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResendEmailConfirmationAsync(resendDto.Email);

            if (result.IsSuccess)
                return Ok(new { message = "Confirmation email sent successfully" });

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = AuthHelper.GetCurrentUserId(User);
            var result = await _authService.GetCurrentUserAsync(userId);

            if (result.IsSuccess)
                return Ok(result.Data);

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = AuthHelper.GetCurrentUserId(User);
            var result = await _authService.UpdateUserAsync(userId, updateUserDto);

            if (result.IsSuccess)
                return Ok(result.Data);

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("check-auth")]
        public async Task<IActionResult> CheckAuth()
        {
            try
            {
                var (accessToken, refreshToken) = _tokenService.GetTokensFromCookies();

                if (string.IsNullOrEmpty(accessToken))
                {
                    return Ok(new { isAuthenticated = false });
                }

                // Try to validate current token
                var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
                var email = principal.Identity?.Name;

                if (string.IsNullOrEmpty(email))
                {
                    return Ok(new { isAuthenticated = false });
                }

                var userId = AuthHelper.GetCurrentUserId(User);
                var userResult = await _authService.GetCurrentUserAsync(userId);

                if (userResult.IsSuccess)
                {
                    return Ok(new
                    {
                        isAuthenticated = true,
                        user = new CookieAuthResponseDto
                        {
                            UserId = userResult.Data.Id,
                            Email = userResult.Data.Email,
                            FirstName = userResult.Data.FirstName,
                            LastName = userResult.Data.LastName,
                            FullName = userResult.Data.FullName,
                            Role = userResult.Data.Role,
                            EmailConfirmed = userResult.Data.EmailConfirmed
                        }
                    });
                }

                return Ok(new { isAuthenticated = false });
            }
            catch
            {
                return Ok(new { isAuthenticated = false });
            }
        }
    }
}
