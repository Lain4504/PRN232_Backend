using BookStore.Data.Model;

namespace BookStore.Services.IServices
{
    public interface IAuthService
    {
        Task<(bool Success, User? User, string Message)> LoginAsync(string emailOrUsername, string password, string? ipAddress = null);
        Task<(bool Success, string? AccessToken, string? RefreshToken, string Message)> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
        Task<bool> LogoutAsync(string refreshToken, string? ipAddress = null);
        Task<bool> LogoutAllAsync(string userId);
        Task<bool> RevokeTokenAsync(string token, string? ipAddress = null, string? reason = null);
        Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(string userId);
        Task<bool> ValidatePasswordAsync(string password, string passwordHash);
        Task<string> HashPasswordAsync(string password);
        Task<RefreshToken> SaveRefreshTokenAsync(RefreshToken refreshToken);
        Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    }
}
