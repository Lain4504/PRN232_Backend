using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Common;
using AISAM.API.Utils;
using AISAM.Common.Dtos;


namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;
        private readonly IUserService _userService;
        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger, IUserService userService)
        {
            _notificationService = notificationService;
            _logger = logger;
            _userService = userService;
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<NotificationResponseDto>>> GetById(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var notification = await _notificationService.GetByIdAsync(id);
                if (notification == null)
                {
                    return NotFound(GenericResponse<NotificationResponseDto>.CreateError("Thông báo không tồn tại"));
                }

                if (notification.UserId != userId)
                {
                    return Forbid("Bạn chỉ có thể xem thông báo của chính mình");
                }

                return Ok(GenericResponse<NotificationResponseDto>.CreateSuccess(notification, "Lấy thông báo thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<NotificationResponseDto>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification by ID: {NotificationId}", id);
                return StatusCode(500, GenericResponse<NotificationResponseDto>.CreateError(
                    "Đã xảy ra lỗi khi lấy thông báo"
                ));
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetNotifications(
            [FromQuery] bool unread = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var request = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _notificationService.GetPagedNotificationsAsync(userId, request, unread);
                var message = unread ? "Lấy danh sách thông báo chưa đọc thành công" : "Lấy danh sách thông báo thành công";
                
                return Ok(GenericResponse<PagedResult<NotificationListDto>>.CreateSuccess(result, message));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<PagedResult<NotificationListDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, GenericResponse<PagedResult<NotificationListDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy danh sách thông báo"
                ));
            }
        }

        public class BulkMarkReadRequest { public IEnumerable<Guid> Ids { get; set; } = Array.Empty<Guid>(); }

        [HttpPost("{id}/read")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<bool>>> MarkAsRead(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var notification = await _notificationService.GetByIdAsync(id);
                if (notification == null)
                {
                    return NotFound(GenericResponse<bool>.CreateError("Thông báo không tồn tại"));
                }
                if (notification.UserId != userId)
                {
                    return Forbid("Bạn chỉ có thể đánh dấu thông báo của chính mình");
                }

                var ok = await _notificationService.MarkAsReadAsync(id);
                return Ok(GenericResponse<bool>.CreateSuccess(ok, ok ? "Đánh dấu đã đọc thành công" : "Không thể đánh dấu đã đọc"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", id);
                return StatusCode(500, GenericResponse<bool>.CreateError(
                    "Đã xảy ra lỗi khi cập nhật thông báo"
                ));
            }
        }

        [HttpPost("read/bulk")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<int>>> BulkMarkAsRead([FromBody] BulkMarkReadRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var count = await _notificationService.MarkAsReadBulkAsync(request.Ids, userId);
                return Ok(GenericResponse<int>.CreateSuccess(count, $"Đã đánh dấu đã đọc {count} thông báo"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<int>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk marking notifications as read");
                return StatusCode(500, GenericResponse<int>.CreateError(
                    "Đã xảy ra lỗi khi cập nhật thông báo"
                ));
            }
        }

    }
}
