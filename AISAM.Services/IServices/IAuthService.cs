using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IAuthService
    {
        Task<TokenResponse> RegisterAsync(RegisterRequest request, string? userAgent, string? ipAddress);
        Task<TokenResponse> LoginAsync(LoginRequest request, string? userAgent, string? ipAddress);
        Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? userAgent, string? ipAddress);
        Task LogoutAsync(Guid userId, string? refreshToken);
        Task LogoutAllSessionsAsync(Guid userId);
        Task<List<SessionDto>> GetActiveSessionsAsync(Guid userId);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        Task<bool> VerifyEmailAsync(string token);
        Task<bool> ResendEmailVerificationAsync(string email);
        Task<TokenResponse> GoogleLoginAsync(string idToken, string? userAgent, string? ipAddress);
    }
}
