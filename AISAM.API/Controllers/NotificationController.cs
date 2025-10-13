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

        /// <summary>
        /// Get notification by ID (auto mark as read when accessed)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<NotificationResponseDto>>> GetById(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var user = await _userService.GetByIdAsync(userId);
                var isAdmin = user.Role == UserRoleEnum.Admin;

                var notification = await _notificationService.GetByIdForUserAsync(id, userId, isAdmin);
                if (notification == null)
                {
                    return NotFound(GenericResponse<NotificationResponseDto>.CreateError("Thông báo không tồn tại"));
                }

                // Auto mark as read when user accesses the notification (simulates clicking)
                if (!notification.IsRead)
                {
                    await _notificationService.MarkAsReadAsync(id);
                    notification.IsRead = true; // Update the returned object
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

        /// <summary>
        /// Get notifications by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<IEnumerable<NotificationResponseDto>>>> GetByUserId(Guid userId)
        {
            try
            {
                var currentUserId = UserClaimsHelper.GetUserIdOrThrow(User);
                var user = await _userService.GetByIdAsync(userId);

                // Users can only view their own notifications
                if (currentUserId != userId)
                {
                    return Forbid("Bạn chỉ có thể xem thông báo của chính mình");
                }

                var notifications = await _notificationService.GetByUserIdAsync(userId);
                return Ok(GenericResponse<IEnumerable<NotificationResponseDto>>.CreateSuccess(notifications, "Lấy danh sách thông báo thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<IEnumerable<NotificationResponseDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user: {UserId}", userId);
                return StatusCode(500, GenericResponse<IEnumerable<NotificationResponseDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy danh sách thông báo"
                ));
            }
        }

        /// <summary>
        /// Get unread notifications by user ID
        /// </summary>
        [HttpGet("user/{userId}/unread")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<IEnumerable<NotificationResponseDto>>>> GetUnreadByUserId(Guid userId)
        {
            try
            {
                var currentUserId = UserClaimsHelper.GetUserIdOrThrow(User);
                var user = await _userService.GetByIdAsync(userId);
                var isAdmin = user.Role == UserRoleEnum.Admin;

                // Users can only view their own notifications
                if (currentUserId != userId)
                {
                    return Forbid("Bạn chỉ có thể xem thông báo của chính mình");
                }

                var notifications = await _notificationService.GetUnreadByUserIdAsync(userId);
                return Ok(GenericResponse<IEnumerable<NotificationResponseDto>>.CreateSuccess(notifications, "Lấy danh sách thông báo chưa đọc thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<IEnumerable<NotificationResponseDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notifications for user: {UserId}", userId);
                return StatusCode(500, GenericResponse<IEnumerable<NotificationResponseDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy danh sách thông báo chưa đọc"
                ));
            }
        }

        /// <summary>
        /// Get paginated notifications for current user
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<NotificationListDto>>>> GetPagedNotifications(
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

                var result = await _notificationService.GetPagedNotificationsAsync(userId, request);
                return Ok(GenericResponse<PagedResult<NotificationListDto>>.CreateSuccess(result, "Lấy danh sách thông báo thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<PagedResult<NotificationListDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated notifications");
                return StatusCode(500, GenericResponse<PagedResult<NotificationListDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy danh sách thông báo"
                ));
            }
        }


        /// <summary>
        /// Create new notification (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GenericResponse<NotificationResponseDto>>> Create(CreateNotificationRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var user = await _userService.GetByIdAsync(userId);
                var isAdmin = user.Role == UserRoleEnum.Admin;
                // Check if user is admin
                if (!isAdmin)
                {
                    return Forbid("Only administrators can create notifications");
                }

                var notification = await _notificationService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = notification.Id },
                    GenericResponse<NotificationResponseDto>.CreateSuccess(notification, "Tạo thông báo thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, GenericResponse<NotificationResponseDto>.CreateError(
                    "Đã xảy ra lỗi khi tạo thông báo"
                ));
            }
        }

        /// <summary>
        /// Create system notification (Admin only)
        /// </summary>
        [HttpPost("system")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<IEnumerable<NotificationResponseDto>>>> CreateSystemNotification(CreateSystemNotificationRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var user = await _userService.GetByIdAsync(userId);
                var isAdmin = user.Role == UserRoleEnum.Admin;
                // Check if user is admin
                if (!isAdmin)
                {
                    return Forbid("Only administrators can create system notifications");
                }

                var notifications = await _notificationService.CreateSystemNotificationAsync(request);
                return Ok(GenericResponse<IEnumerable<NotificationResponseDto>>.CreateSuccess(
                    notifications, $"Tạo thành công {notifications.Count()} thông báo hệ thống"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system notification");
                return StatusCode(500, GenericResponse<IEnumerable<NotificationResponseDto>>.CreateError(
                    "Đã xảy ra lỗi khi tạo thông báo hệ thống"
                ));
            }
        }

        /// <summary>
        /// Update notification
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<NotificationResponseDto>>> Update(Guid id, UpdateNotificationRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var user = await _userService.GetByIdAsync(userId);
                var isAdmin = user.Role == UserRoleEnum.Admin;

                var notification = await _notificationService.UpdateForUserAsync(id, request, userId, isAdmin);
                if (notification == null)
                {
                    return NotFound(GenericResponse<NotificationResponseDto>.CreateError("Thông báo không tồn tại"));
                }

                return Ok(GenericResponse<NotificationResponseDto>.CreateSuccess(notification, "Cập nhật thông báo thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, GenericResponse<NotificationResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification: {NotificationId}", id);
                return StatusCode(500, GenericResponse<NotificationResponseDto>.CreateError(
                    "Đã xảy ra lỗi khi cập nhật thông báo"
                ));
            }
        }



        /// <summary>
        /// Delete notification
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<bool>>> Delete(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var user = await _userService.GetByIdAsync(userId);
                var isAdmin = user.Role == UserRoleEnum.Admin;
                var result = await _notificationService.DeleteForUserAsync(id, userId, isAdmin);
                if (!result)
                {
                    return NotFound(GenericResponse<bool>.CreateError("Thông báo không tồn tại"));
                }

                return Ok(GenericResponse<bool>.CreateSuccess(true, "Xóa thông báo thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, GenericResponse<bool>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification: {NotificationId}", id);
                return StatusCode(500, GenericResponse<bool>.CreateError(
                    "Đã xảy ra lỗi khi xóa thông báo"
                ));
            }
        }

        /// <summary>
        /// Manually trigger cleanup of old notifications (Admin only)
        /// </summary>
        [HttpDelete("cleanup")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<int>>> CleanupOldNotifications()
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var user = await _userService.GetByIdAsync(userId);
                var isAdmin = user.Role == UserRoleEnum.Admin;

                var deletedCount = await _notificationService.DeleteOldNotificationsAsync(30);

                return Ok(GenericResponse<int>.CreateSuccess(deletedCount,
                    $"Đã xóa {deletedCount} thông báo cũ hơn 30 ngày"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual notification cleanup");
                return StatusCode(500, GenericResponse<int>.CreateError(
                    "Đã xảy ra lỗi khi dọn dẹp thông báo cũ"
                ));
            }
        }
    }
}
