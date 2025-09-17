using BookStore.Common.Models;
using BookStore.Data.Model;

namespace BookStore.Services.IServices
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
    }
}