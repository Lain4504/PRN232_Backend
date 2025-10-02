using Microsoft.AspNetCore.Mvc;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using Microsoft.AspNetCore.Authorization;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/content")]
    public class ContentController : ControllerBase
    {
        private readonly IContentService _contentService;
        private readonly ILogger<ContentController> _logger;

        public ContentController(IContentService contentService, ILogger<ContentController> logger)
        {
            _contentService = contentService;
            _logger = logger;
        }

        /// <summary>
        /// Create new content and optionally publish it immediately
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ContentResponseDto>>> CreateContent([FromBody] CreateContentRequest request)
        {
            try
            {

                var result = await _contentService.CreateContentAsync(request);
                
                var message = request.PublishImmediately 
                    ? "Tạo và đăng bài thành công" 
                    : "Tạo nội dung thành công";

                return Ok(GenericResponse<ContentResponseDto>.CreateSuccess(result, message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for content creation");
                return BadRequest(GenericResponse<ContentResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating content");
                return StatusCode(500, GenericResponse<ContentResponseDto>.CreateError(
                    "Đã xảy ra lỗi khi tạo nội dung"
                ));
            }
        }

        /// <summary>
        /// Publish existing content to a social integration
        /// </summary>
        [HttpPost("{contentId}/publish/{integrationId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PublishResultDto>>> PublishContent(
            Guid contentId, 
            Guid integrationId)
        {
            try
            {
                var result = await _contentService.PublishContentAsync(contentId, integrationId);
                
                var message = result.Success 
                    ? "Đăng bài thành công" 
                    : "Đăng bài thất bại";

                return Ok(GenericResponse<PublishResultDto>.CreateSuccess(result, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing content {ContentId} to integration {IntegrationId}", 
                    contentId, integrationId);
                return StatusCode(500, GenericResponse<PublishResultDto>.CreateError(
                    "Đã xảy ra lỗi khi đăng bài"
                ));
            }
        }

        /// <summary>
        /// Soft delete content
        /// </summary>
        [HttpDelete("{contentId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> SoftDelete(Guid contentId)
        {
            try
            {
                var ok = await _contentService.SoftDeleteAsync(contentId);
                if (!ok)
                {
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy nội dung hoặc đã bị xóa"));
                }
                return Ok(GenericResponse<object>.CreateSuccess(null, "Xóa mềm nội dung thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting content {ContentId}", contentId);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi khi xóa nội dung"));
            }
        }

        /// <summary>
        /// Restore soft-deleted content (status resets to DRAFT)
        /// </summary>
        [HttpPost("{contentId}/restore")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> Restore(Guid contentId)
        {
            try
            {
                var ok = await _contentService.RestoreAsync(contentId);
                if (!ok)
                {
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy nội dung hoặc không ở trạng thái đã xóa"));
                }
                return Ok(GenericResponse<object>.CreateSuccess(null, "Khôi phục nội dung thành công (trạng thái DRAFT)"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring content {ContentId}", contentId);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi khi khôi phục nội dung"));
            }
        }

        /// <summary>
        /// Hard delete content permanently
        /// </summary>
        [HttpDelete("{contentId}/hard")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> HardDelete(Guid contentId)
        {
            try
            {
                var ok = await _contentService.HardDeleteAsync(contentId);
                if (!ok)
                {
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy nội dung"));
                }
                return Ok(GenericResponse<object>.CreateSuccess(null, "Xóa vĩnh viễn nội dung thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting content {ContentId}", contentId);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi khi xóa vĩnh viễn nội dung"));
            }
        }

        /// <summary>
        /// Get content by ID
        /// </summary>
        [HttpGet("{contentId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ContentResponseDto>>> GetContent(Guid contentId)
        {
            try
            {
                var content = await _contentService.GetContentByIdAsync(contentId);
                
                if (content == null)
                {
                    return NotFound(GenericResponse<ContentResponseDto>.CreateError("Không tìm thấy nội dung"));
                }

                return Ok(GenericResponse<ContentResponseDto>.CreateSuccess(content, "Lấy thông tin nội dung thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content {ContentId}", contentId);
                return StatusCode(500, GenericResponse<ContentResponseDto>.CreateError(
                    "Đã xảy ra lỗi khi lấy thông tin nội dung"
                ));
            }
        }

        /// <summary>
        /// Get paginated contents by brand
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<ContentResponseDto>>>> GetContentsByBrand(
            [FromQuery] Guid brandId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true,
            [FromQuery] AdTypeEnum? adType = null,
            [FromQuery] bool onlyDeleted = false,
            [FromQuery] ContentStatusEnum? status = null)
        {
            try
            {
                if (brandId == Guid.Empty)
                {
                    return BadRequest(GenericResponse<PagedResult<ContentResponseDto>>.CreateError("brandId là bắt buộc"));
                }

                var request = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _contentService.GetPagedContentsByBrandAsync(
                    brandId,
                    request,
                    adType,
                    onlyDeleted,
                    status);

                return Ok(GenericResponse<PagedResult<ContentResponseDto>>.CreateSuccess(
                    result,
                    "Lấy danh sách nội dung theo brand thành công"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contents by brand {BrandId}", brandId);
                return StatusCode(500, GenericResponse<PagedResult<ContentResponseDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy danh sách nội dung"
                ));
            }
        }
    }
}
