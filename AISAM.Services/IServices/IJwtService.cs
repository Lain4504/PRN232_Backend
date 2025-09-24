using System.Security.Claims;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        bool IsTokenBlacklisted(string token);
        Task<string> BlacklistTokenAsync(string token, string? reason = null, string? userId = null);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken, string userId);
        Task<string> RevokeRefreshTokenAsync(string refreshToken, string? revokedByIp = null, string? reason = null);
        string? GetUserIdFromToken(string token);
        string? GetJtiFromToken(string token);
        DateTime? GetExpirationFromToken(string token);
    }
}
