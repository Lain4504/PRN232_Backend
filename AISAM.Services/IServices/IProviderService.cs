using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface IProviderService
    {
        string ProviderName { get; }
        
        /// <summary>
        /// Generate OAuth authorization URL
        /// </summary>
        Task<string> GetAuthUrlAsync(string state, string redirectUri);
        
        /// <summary>
        /// Exchange authorization code for access token and user info
        /// </summary>
        Task<SocialAccountDto> ExchangeCodeAsync(string code, string redirectUri);
        
        /// <summary>
        /// Get available targets (pages, profiles, channels) for the user
        /// </summary>
        Task<IEnumerable<SocialTargetDto>> GetTargetsAsync(string accessToken);

        /// <summary>
        /// Server-side only: For given provider target IDs (e.g., Facebook Page IDs),
        /// return a mapping to target access tokens using the provided user access token.
        /// Only used during linking to persist per-target tokens.
        /// </summary>
        Task<Dictionary<string, string>> GetTargetAccessTokensAsync(string userAccessToken, IEnumerable<string> providerTargetIds);
        
        /// <summary>
        /// [TEMP] Facebook-specific: Get page information from a Page Access Token.
        /// Used only by temporary endpoint /auth/link-page-token to validate/link a Page.
        /// Will be removed when the official OAuth flow replaces the temp API.
        /// </summary>
        Task<FacebookPageInfo> GetPageInfoFromTokenAsync(string pageAccessToken);
        
        /// <summary>
        /// [TEMP] Facebook-specific: Get user information from a User Access Token.
        /// Supporting method for the temporary /auth/link-page-token flow.
        /// Will be deleted once the temp API is retired.
        /// </summary>
        Task<FacebookUserInfo> GetUserInfoFromTokenAsync(string userAccessToken);
        
        /// <summary>
        /// Publish a post to the specified target
        /// </summary>
        Task<PublishResultDto> PublishAsync(SocialAccount account, SocialTarget target, PostDto post);
        
        /// <summary>
        /// Refresh access token if needed
        /// </summary>
        Task RefreshTokenIfNeededAsync(SocialAccount account);
        
        /// <summary>
        /// Validate if the access token is still valid
        /// </summary>
        Task<bool> ValidateTokenAsync(string accessToken);
    }

    public class PostDto
    {
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Metadata { get; set; }
    }

    public class PublishResultDto
    {
        public bool Success { get; set; }
        public string? ProviderPostId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? PostedAt { get; set; }
        // If provider had to refresh/fetch a page token to succeed, return it so callers can persist
        public string? RefreshedTargetAccessToken { get; set; }
    }
}