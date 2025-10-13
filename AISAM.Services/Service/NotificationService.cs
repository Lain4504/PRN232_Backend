using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Services.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IContentRepository _contentRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;

        public NotificationService(
            INotificationRepository notificationRepository,
            IContentRepository contentRepository,
            IBrandRepository brandRepository,
            ITeamMemberRepository teamMemberRepository)
        {
            _notificationRepository = notificationRepository;
            _contentRepository = contentRepository;
            _brandRepository = brandRepository;
            _teamMemberRepository = teamMemberRepository;
        }
        public async Task<NotificationResponseDto?> GetByIdAsync(Guid id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
                return null;

            return MapToResponseDto(notification);
        }
        public async Task<NotificationResponseDto?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin = false)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
                return null;

            // Check if user has access to this notification
            if (!isAdmin && notification.UserId != userId)
            {
                return null; // User can only access their own notifications
            }

            return MapToResponseDto(notification);
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetByUserIdAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetUnreadByUserIdAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            return notifications.Select(MapToResponseDto);
        }

        public async Task<NotificationResponseDto> CreateAsync(CreateNotificationRequest request)
        {
            var notification = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                TargetId = request.TargetId,
                TargetType = request.TargetType
            };

            var createdNotification = await _notificationRepository.CreateAsync(notification);
            return MapToResponseDto(createdNotification);
        }

        public async Task<IEnumerable<NotificationResponseDto>> CreateSystemNotificationAsync(CreateSystemNotificationRequest request)
        {
            var notifications = new List<Notification>();
            var targetUsers = await GetTargetUsersAsync(request);

            foreach (var userId in targetUsers)
            {
                var title = ReplaceTemplateVariables(request.TitleTemplate, request.TemplateVariables);
                var message = ReplaceTemplateVariables(request.MessageTemplate, request.TemplateVariables);

                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = request.Type,
                    TargetId = request.TargetId,
                    TargetType = request.TargetType,
                    IsRead = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                notifications.Add(notification);
            }

            // Batch create notifications
            var createdNotifications = new List<Notification>();
            foreach (var notification in notifications)
            {
                var created = await _notificationRepository.CreateAsync(notification);
                createdNotifications.Add(created);
            }

            return createdNotifications.Select(MapToResponseDto);
        }

        public async Task<NotificationResponseDto?> UpdateAsync(Guid id, UpdateNotificationRequest request)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
                return null;

            // Update only non-null fields
            if (request.Title != null)
                notification.Title = request.Title;
            if (request.Message != null)
                notification.Message = request.Message;
            if (request.Type.HasValue)
                notification.Type = request.Type.Value;
            if (request.TargetId.HasValue)
                notification.TargetId = request.TargetId;
            if (request.TargetType != null)
                notification.TargetType = request.TargetType;
            if (request.IsRead.HasValue)
                notification.IsRead = request.IsRead.Value;

            var updatedNotification = await _notificationRepository.UpdateAsync(notification);
            return MapToResponseDto(updatedNotification);
        }

        public async Task<NotificationResponseDto?> UpdateForUserAsync(Guid id, UpdateNotificationRequest request, Guid userId, bool isAdmin = false)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
                return null;

            // Check if user has access to this notification
            if (!isAdmin && notification.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ có thể cập nhật thông báo của chính mình");
            }

            // Update only non-null fields
            if (request.Title != null)
                notification.Title = request.Title;
            if (request.Message != null)
                notification.Message = request.Message;
            if (request.Type.HasValue)
                notification.Type = request.Type.Value;
            if (request.TargetId.HasValue)
                notification.TargetId = request.TargetId;
            if (request.TargetType != null)
                notification.TargetType = request.TargetType;
            if (request.IsRead.HasValue)
                notification.IsRead = request.IsRead.Value;

            var updatedNotification = await _notificationRepository.UpdateAsync(notification);
            return MapToResponseDto(updatedNotification);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _notificationRepository.DeleteAsync(id);
        }

        public async Task<bool> DeleteForUserAsync(Guid id, Guid userId, bool isAdmin = false)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
                return false;

            // Check if user has access to this notification
            if (notification.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ có thể xóa thông báo của chính mình");
            }

            return await _notificationRepository.DeleteAsync(id);
        }

        public async Task<bool> MarkAsReadAsync(Guid id)
        {
            return await _notificationRepository.MarkAsReadAsync(id);
        }

        public async Task<int> DeleteOldNotificationsAsync(int daysOld = 30)
        {
            return await _notificationRepository.DeleteOldNotificationsAsync(daysOld);
        }

        public async Task<PagedResult<NotificationListDto>> GetPagedNotificationsAsync(Guid userId, PaginationRequest request)
        {
            // Validate pagination parameters
            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            if (request.PageSize > 100) request.PageSize = 100; // Limit max page size

            return await _notificationRepository.GetPagedNotificationsAsync(userId, request);
        }

        private static NotificationResponseDto MapToResponseDto(Notification notification)
        {
            return new NotificationResponseDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                TargetId = notification.TargetId,
                TargetType = notification.TargetType,
                IsRead = notification.IsRead,
                IsDeleted = notification.IsDeleted,
                CreatedAt = notification.CreatedAt,
                UserEmail = notification.User?.Email ?? ""
            };
        }

        private async Task<IEnumerable<Guid>> GetTargetUsersAsync(CreateSystemNotificationRequest request)
        {
            var targetUsers = new HashSet<Guid>();

            // 1. Add owner của content/brand nếu có ContentId hoặc BrandId
            if (request.ContentId.HasValue)
            {
                var content = await _contentRepository.GetByIdAsync(request.ContentId.Value);
                if (content != null)
                {
                    var brand = await _brandRepository.GetByIdAsync(content.BrandId);
                    if (brand != null)
                    {
                        targetUsers.Add(brand.UserId);
                    }
                }
            }
            else if (request.BrandId.HasValue)
            {
                var brand = await _brandRepository.GetByIdAsync(request.BrandId.Value);
                if (brand != null)
                {
                    targetUsers.Add(brand.UserId);
                }
            }

            // 2. Add approver nếu có ApproverId
            if (request.ApproverId.HasValue)
            {
                targetUsers.Add(request.ApproverId.Value);
            }

            // 3. Add team members nếu có TeamMemberIds hoặc SendToAllTeamMembers
            if (request.TeamMemberIds != null && request.TeamMemberIds.Any())
            {
                foreach (var teamMemberId in request.TeamMemberIds)
                {
                    var teamMember = await _teamMemberRepository.GetByIdAsync(teamMemberId);
                    if (teamMember != null)
                    {
                        targetUsers.Add(teamMember.UserId);
                    }
                }
            }
            else if (request.SendToAllTeamMembers && request.ContentId.HasValue)
            {
                // Nếu không chỉ định team members cụ thể và có ContentId,
                // tìm tất cả team members của vendor có liên quan đến content
                var content = await _contentRepository.GetByIdAsync(request.ContentId.Value);
                if (content != null)
                {
                    // Tìm tất cả teams của vendor và lấy tất cả team members
                    // Vì không có ITeamRepository, chúng ta cần một cách tiếp cận khác
                    // Có thể cần thêm ITeamRepository hoặc sử dụng cách khác để lấy team members

                    // Hiện tại tạm thời bỏ qua logic này và để lại comment
                }
            }
            else if (request.VendorId.HasValue)
            {
                // Nếu có VendorId được chỉ định, tìm tất cả team members của vendor đó
                // Vì không có ITeamRepository, chúng ta cần một cách tiếp cận khác
                // Có thể cần thêm ITeamRepository hoặc sử dụng cách khác để lấy team members

                // Hiện tại tạm thời bỏ qua logic này và để lại comment
            }
            return targetUsers;
        }
        private static string ReplaceTemplateVariables(string template, Dictionary<string, string>? variables)
        {
            if (variables == null || !variables.Any())
                return template;

            var result = template;
            foreach (var variable in variables)
            {
                result = result.Replace($"{{{variable.Key}}}", variable.Value);
            }

            return result;
        }
        
    }
}