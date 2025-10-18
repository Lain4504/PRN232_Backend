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

        public NotificationService(
            INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }
        public async Task<NotificationResponseDto?> GetByIdAsync(Guid id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
                return null;

            return MapToResponseDto(notification);
        }

        public async Task<PagedResult<NotificationListDto>> GetPagedNotificationsAsync(Guid userId, PaginationRequest request, bool unreadOnly = false)
        {
            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            if (request.PageSize > 100) request.PageSize = 100;

            return await _notificationRepository.GetPagedNotificationsAsync(userId, request, unreadOnly);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(Guid id)
        {
            return await _notificationRepository.MarkAsReadAsync(id);
        }

        public async Task<int> MarkAsReadBulkAsync(IEnumerable<Guid> ids, Guid profileId)
        {
            return await _notificationRepository.MarkAsReadBulkAsync(ids, profileId);
        }


        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            return await _notificationRepository.MarkAllAsReadAsync(userId);
        }
        public async Task<NotificationResponseDto> CreateAsync(CreateNotificationRequest request)
        {
            var notification = new Notification
            {
                ProfileId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                TargetId = request.TargetId,
                TargetType = request.TargetType
            };

            var createdNotification = await _notificationRepository.CreateAsync(notification);
            return MapToResponseDto(createdNotification);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null || notification.ProfileId != userId)
                return false;

            return await _notificationRepository.DeleteAsync(id);
        }

        public async Task<int> DeleteOldNotificationsAsync(int daysOld = 30)
        {
            return await _notificationRepository.DeleteOldNotificationsAsync(daysOld);
        }

        private static NotificationResponseDto MapToResponseDto(Notification notification)
        {
            return new NotificationResponseDto
            {
                Id = notification.Id,
                ProfileId = notification.ProfileId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                TargetId = notification.TargetId,
                TargetType = notification.TargetType,
                IsRead = notification.IsRead,
                IsDeleted = notification.IsDeleted,
                CreatedAt = notification.CreatedAt,
                UserEmail = notification.Profile?.User?.Email ?? ""
            };
        }

    }
}