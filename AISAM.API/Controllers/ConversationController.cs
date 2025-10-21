using Microsoft.AspNetCore.Mvc;
using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using AISAM.API.Utils;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/conversations")]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<ConversationController> _logger;

        public ConversationController(IConversationService conversationService, ILogger<ConversationController> logger)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        /// <summary>
        /// Get user's conversations with pagination
        /// GET api/conversations?page=1&pageSize=10
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<ConversationResponseDto>>>> GetConversations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true)
        {
            try
            {
                var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

                var paginationRequest = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _conversationService.GetUserConversationsAsync(profileId, paginationRequest);
                return Ok(GenericResponse<PagedResult<ConversationResponseDto>>.CreateSuccess(result, "Conversations retrieved successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<PagedResult<ConversationResponseDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations");
                return StatusCode(500, GenericResponse<PagedResult<ConversationResponseDto>>.CreateError("Đã xảy ra lỗi khi lấy danh sách cuộc trò chuyện"));
            }
        }

        /// <summary>
        /// Get conversation details by ID
        /// GET api/conversations/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ConversationDetailDto>>> GetConversationById(Guid id)
        {
            try
            {
                var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
                var conversation = await _conversationService.GetConversationByIdAsync(id, profileId);

                if (conversation == null)
                    return NotFound(GenericResponse<ConversationDetailDto>.CreateError("Không tìm thấy cuộc trò chuyện"));

                return Ok(GenericResponse<ConversationDetailDto>.CreateSuccess(conversation, "Conversation details retrieved successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<ConversationDetailDto>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation {ConversationId}", id);
                return StatusCode(500, GenericResponse<ConversationDetailDto>.CreateError("Đã xảy ra lỗi khi lấy chi tiết cuộc trò chuyện"));
            }
        }

        /// <summary>
        /// Delete conversation (soft delete)
        /// DELETE api/conversations/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> DeleteConversation(Guid id)
        {
            try
            {
                var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
                var success = await _conversationService.DeleteConversationAsync(id, profileId);

                if (!success)
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy cuộc trò chuyện hoặc không có quyền truy cập"));

                return Ok(GenericResponse<object>.CreateSuccess(null, "Conversation deleted successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<object>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {ConversationId}", id);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi khi xóa cuộc trò chuyện"));
            }
        }
    }
}