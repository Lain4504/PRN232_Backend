using AISAM.Common.Models;
using AISAM.Data.Enumeration;
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
        Task<IEnumerable<AvailableTargetDto>> GetTargetsAsync(string accessToken);

        /// <summary>
        /// Server-side only: For given provider target IDs (e.g., Facebook Page IDs),
        /// return a mapping to target access tokens using the provided user access token.
        /// Only used during linking to persist per-target tokens.
        /// </summary>
        Task<Dictionary<string, string>> GetTargetAccessTokensAsync(string userAccessToken, IEnumerable<string> providerTargetIds);
        
        /// <summary>
        /// Publish a post to the specified social integration
        /// </summary>
        Task<PublishResultDto> PublishAsync(SocialAccount account, SocialIntegration integration, PostDto post);
        
        /// <summary>
        /// Refresh access token if needed
        /// </summary>
        Task RefreshTokenIfNeededAsync(SocialAccount account);
        
        /// <summary>
        /// Validate if the access token is still valid
        /// </summary>
        Task<bool> ValidateTokenAsync(string accessToken);
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