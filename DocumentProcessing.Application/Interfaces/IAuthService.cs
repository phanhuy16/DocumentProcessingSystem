using DocumentProcessing.Application.Common;
using DocumentProcessing.Application.DTOs.Auth;

namespace DocumentProcessing.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
        Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto, string ipAddress, string userAgent);
        Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string ipAddress, string userAgent);
        Task<Result<bool>> LogoutAsync(string sessionId);
        Task<Result<bool>> LogoutAllAsync(Guid userId);
        Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
        Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<Result<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<Result<bool>> ConfirmEmailAsync(string email, string token);
        Task<Result<bool>> ResendEmailConfirmationAsync(string email);
        Task<Result<UserDto>> GetCurrentUserAsync(Guid userId);
        Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto);
    }
}
