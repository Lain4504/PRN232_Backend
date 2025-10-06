using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface ISocialService
    {
        /// <summary>
        /// Get authorization URL for a provider. Optionally include userId in callback URL.
        /// </summary>
        Task<AuthUrlResponse> GetAuthUrlAsync(string provider, string? state = null, Guid? userId = null);
        
        /// <summary>
        /// Link a social account to a user
        /// </summary>
        Task<SocialAccountDto> LinkAccountAsync(LinkSocialAccountRequest request);
        
        /// <summary>
        /// Unlink a social account from a user
        /// </summary>
        Task<bool> UnlinkAccountAsync(Guid userId, Guid socialAccountId);
        
        /// <summary>
        /// Get all social accounts for a user
        /// </summary>
        Task<IEnumerable<SocialAccountDto>> GetUserAccountsAsync(Guid userId);
        
        /// <summary>
        /// Get all targets for a social account
        /// </summary>
        Task<IEnumerable<SocialTargetDto>> GetAccountTargetsAsync(Guid socialAccountId);

        /// <summary>
        /// List available targets for a specific social account
        /// </summary>
        Task<IEnumerable<AvailableTargetDto>> ListAvailableTargetsForAccountAsync(Guid socialAccountId);


        /// <summary>
        /// Link selected targets to a specific social account
        /// </summary>
        Task<SocialAccountDto> LinkSelectedTargetsForAccountAsync(Guid socialAccountId, LinkSelectedTargetsRequest request);

        /// <summary>
        /// Get a social account by ID
        /// </summary>
        Task<SocialAccountDto?> GetSocialAccountByIdAsync(Guid socialAccountId);

        /// <summary>
        /// Unlink a specific target/page (social integration) from the user's social account
        /// </summary>
        Task<bool> UnlinkTargetAsync(Guid userId, Guid socialIntegrationId);
    }
}