using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Services.IServices;
using AISAM.Common.Models;
using System.Security.Claims;

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
        /// Get content by ID
        /// </summary>
        [HttpGet("{contentId}")]
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
        /// Get all contents for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<GenericResponse<IEnumerable<ContentResponseDto>>>> GetUserContents()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return Unauthorized(GenericResponse<IEnumerable<ContentResponseDto>>.CreateError("Token không hợp lệ"));
                }

                var contents = await _contentService.GetUserContentsAsync(userId);
                
                return Ok(GenericResponse<IEnumerable<ContentResponseDto>>.CreateSuccess(
                    contents, 
                    "Lấy danh sách nội dung thành công"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contents");
                return StatusCode(500, GenericResponse<IEnumerable<ContentResponseDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy danh sách nội dung"
                ));
            }
        }
    }
}
