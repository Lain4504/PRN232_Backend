using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface ISocialService
    {
        /// <summary>
        /// Get authorization URL for a provider
        /// </summary>
        Task<AuthUrlResponse> GetAuthUrlAsync(string provider, string? state = null);
        
        /// <summary>
        /// Link a social account to a user
        /// </summary>
        Task<SocialAccountDto> LinkAccountAsync(LinkSocialAccountRequest request);
        
        /// <summary>
        /// Link Facebook Page by access token directly
        /// </summary>
        Task<SocialAccountDto> LinkPageByTokenAsync(LinkPageByTokenRequest request);
        
        /// <summary>
        /// Unlink a social account from a user
        /// </summary>
        Task<bool> UnlinkAccountAsync(int userId, int socialAccountId);
        
        /// <summary>
        /// Get all social accounts for a user
        /// </summary>
        Task<IEnumerable<SocialAccountDto>> GetUserAccountsAsync(int userId);
        
        /// <summary>
        /// Get all targets for a social account
        /// </summary>
        Task<IEnumerable<SocialTargetDto>> GetAccountTargetsAsync(int socialAccountId);
    }
}