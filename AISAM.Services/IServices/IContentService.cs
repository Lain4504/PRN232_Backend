using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface IContentService
    {
        /// <summary>
        /// Create content and optionally publish it to social media
        /// </summary>
        Task<ContentResponseDto> CreateContentAsync(CreateContentRequest request);
        
        /// <summary>
        /// Publish existing content to social integration
        /// </summary>
        Task<PublishResultDto> PublishContentAsync(Guid contentId, Guid integrationId);
        
        /// <summary>
        /// Get content by ID
        /// </summary>
        Task<ContentResponseDto?> GetContentByIdAsync(Guid contentId);
        
        /// <summary>
        /// Get all contents for a user
        /// </summary>
        Task<IEnumerable<ContentResponseDto>> GetUserContentsAsync(Guid userId);
    }
}
