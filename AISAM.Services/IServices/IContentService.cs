using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;

namespace AISAM.Services.IServices
{
    public interface IContentService
    {
        /// <summary>
        /// Create content and optionally publish it to social media
        /// </summary>
        Task<ContentResponseDto> CreateContentAsync(CreateContentRequest request, Guid userId);
        
        /// <summary>
        /// Publish existing content to social integration
        /// </summary>
        Task<PublishResultDto> PublishContentAsync(Guid contentId, Guid integrationId, Guid userId);
        
        /// <summary>
        /// Get content by ID
        /// </summary>
        Task<ContentResponseDto?> GetContentByIdAsync(Guid contentId, Guid userId);
        

        /// <summary>
        /// Get paged contents by brand
        /// </summary>
        Task<PagedResult<ContentResponseDto>> GetPagedContentsByBrandAsync(
            Guid brandId,
            PaginationRequest request,
            AdTypeEnum? adType = null,
            bool onlyDeleted = false,
            ContentStatusEnum? status = null);

        /// <summary>
        /// Soft delete content
        /// </summary>
        Task<bool> SoftDeleteAsync(Guid contentId, Guid userId);

        /// <summary>
        /// Restore soft-deleted content; set status to Draft
        /// </summary>
        Task<bool> RestoreAsync(Guid contentId, Guid userId);

    }
}
