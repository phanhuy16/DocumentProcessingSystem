using DocumentProcessing.Domain.Entities;
using System.Security.Claims;

namespace DocumentProcessing.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        void SetTokensInCookies(string accessToken, string refreshToken, bool rememberMe = false);
        (string? AccessToken, string? RefreshToken) GetTokensFromCookies();
        void ClearTokenCookies();
    }
}
