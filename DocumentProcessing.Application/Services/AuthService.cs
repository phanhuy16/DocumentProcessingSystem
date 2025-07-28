using DocumentProcessing.Application.Common;
using DocumentProcessing.Application.DTOs.Auth;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Domain.Enums;
using DocumentProcessing.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DocumentProcessing.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUserSessionService _userSessionService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
           UserManager<ApplicationUser> userManager,
           SignInManager<ApplicationUser> signInManager,
           RoleManager<ApplicationRole> roleManager,
           ITokenService tokenService,
           IUserSessionService userSessionService,
           ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _userSessionService = userSessionService;
            _logger = logger;
        }

        public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return Result<AuthResponseDto>.Failure("User with this email already exists");
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    Role = UserRole.User,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result<AuthResponseDto>.Failure($"Registration failed: {errors}");
                }

                // Assign default role if it doesn't exist
                await _userManager.AddToRoleAsync(user, UserRole.User.ToString());

                // Generate email confirmation token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Generate JWT token
                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Update user with refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                // Set tokens in cookies
                _tokenService.SetTokensInCookies(accessToken, refreshToken, false);

                var response = new AuthResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Role = user.Role,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiry = DateTime.UtcNow.AddHours(1),
                    EmailConfirmed = user.EmailConfirmed,
                };

                return Result<AuthResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", registerDto.Email);
                return Result<AuthResponseDto>.Failure("Registration failed");
            }
        }

        public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto, string ipAddress, string userAgent)
        {
            try
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return Result<AuthResponseDto>.Failure("Invalid email or password");
                }

                if (!user.IsActive)
                {
                    return Result<AuthResponseDto>.Failure("Account is deactivated");
                }

                // Check password
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    if (result.IsLockedOut)
                    {
                        return Result<AuthResponseDto>.Failure("Account is locked out");
                    }
                    return Result<AuthResponseDto>.Failure("Invalid email or password");
                }

                // Generate JWT token
                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Update user with refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Create session
                var session = await _userSessionService.CreateSessionAsync(user.Id, ipAddress, userAgent);

                // Set tokens in cookies
                _tokenService.SetTokensInCookies(accessToken, refreshToken, loginDto.RememberMe);

                var response = new AuthResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Role = user.Role,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiry = DateTime.UtcNow.AddHours(1),
                    EmailConfirmed = user.EmailConfirmed,
                };

                return Result<AuthResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
                return Result<AuthResponseDto>.Failure("Login failed");
            }
        }

        public async Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string ipAddress, string userAgent)
        {
            try
            {
                // Get tokens from cookies if not provided in request
                var (cookieAccessToken, cookieRefreshToken) = _tokenService.GetTokensFromCookies();

                var accessToken = string.IsNullOrEmpty(refreshTokenDto.AccessToken) ? cookieAccessToken : refreshTokenDto.AccessToken;
                var refreshToken = string.IsNullOrEmpty(refreshTokenDto.RefreshToken) ? cookieRefreshToken : refreshTokenDto.RefreshToken;

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    return Result<AuthResponseDto>.Failure("Access token and refresh token are required");
                }

                var prinicipal = _tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
                var email = prinicipal.Identity?.Name;

                if (string.IsNullOrEmpty(email))
                {
                    return Result<AuthResponseDto>.Failure("Invalid access token");
                }

                var user = await _userManager.FindByEmailAsync(email);
                if(user == null || user.RefreshToken != refreshTokenDto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    _tokenService.ClearTokenCookies();
                    return Result<AuthResponseDto>.Failure("Invalid or expired refresh token");
                }

                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                // Update tokens in cookies
                _tokenService.SetTokensInCookies(newAccessToken, newRefreshToken, false);

                var response = new AuthResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Role = user.Role,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    TokenExpiry = DateTime.UtcNow.AddHours(1),
                    EmailConfirmed = user.EmailConfirmed,
                };

                return Result<AuthResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return Result<AuthResponseDto>.Failure("Token refresh failed");
            }
        }

        public async Task<Result<bool>> LogoutAsync(string sessionId)
        {
            try
            {
                // Clear cookies
                _tokenService.ClearTokenCookies();

                // Clear session if provided
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await _userSessionService.DeleteSessionAsync(sessionId);
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return Result<bool>.Failure("Logout failed");
            }
        }

        public async Task<Result<bool>> LogoutAllAsync(Guid userId)
        {
            try
            {
                // Clear all user sessions
                await _userSessionService.DeleteUserSessionsAsync(userId);

                // Clear refresh token from database
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user != null)
                {
                    user.RefreshToken = string.Empty;
                    user.RefreshTokenExpiryTime = null;
                    await _userManager.UpdateAsync(user);
                }

                // Clear cookies
                _tokenService.ClearTokenCookies();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout all for user {UserId}", userId);
                return Result<bool>.Failure("Logout all failed");
            }
        }

        // Implement other methods (ChangePasswordAsync, ForgotPasswordAsync, etc.) - same as before
        public async Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return Result<bool>.Failure("User not found");
                }

                var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result<bool>.Failure($"Password change failed: {errors}");
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change for user {UserId}", userId);
                return Result<bool>.Failure("Password change failed");
            }
        }

        public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
                if (user == null)
                {
                    return Result<bool>.Success(true);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // TODO: Send email with reset token

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for {Email}", forgotPasswordDto.Email);
                return Result<bool>.Failure("Forgot password failed");
            }
        }

        public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
                if (user == null)
                {
                    return Result<bool>.Failure("Invalid reset token");
                }

                var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result<bool>.Failure($"Password reset failed: {errors}");
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return Result<bool>.Failure("Password reset failed");
            }
        }

        public async Task<Result<bool>> ConfirmEmailAsync(string email, string token)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Result<bool>.Failure("Invalid confirmation token");
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    return Result<bool>.Failure("Email confirmation failed");
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email confirmation for {Email}", email);
                return Result<bool>.Failure("Email confirmation failed");
            }
        }

        public async Task<Result<bool>> ResendEmailConfirmationAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Result<bool>.Failure("User not found");
                }

                if (user.EmailConfirmed)
                {
                    return Result<bool>.Failure("Email already confirmed");
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // TODO: Send email confirmation

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resend email confirmation for {Email}", email);
                return Result<bool>.Failure("Resend email confirmation failed");
            }
        }

        public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return Result<UserDto>.Failure("User not found");
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return Result<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user {UserId}", userId);
                return Result<UserDto>.Failure("Failed to get user");
            }
        }

        public async Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return Result<UserDto>.Failure("User not found");
                }

                user.FirstName = updateUserDto.FirstName;
                user.LastName = updateUserDto.LastName;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result<UserDto>.Failure($"Update failed: {errors}");
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return Result<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return Result<UserDto>.Failure("Failed to update user");
            }
        }
    }
}
