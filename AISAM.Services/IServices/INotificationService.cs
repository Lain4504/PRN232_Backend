using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface INotificationService
    {
        Task<NotificationResponseDto?> GetByIdAsync(Guid id);
        Task<NotificationResponseDto?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin = false);
        Task<IEnumerable<NotificationResponseDto>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<NotificationResponseDto>> GetUnreadByUserIdAsync(Guid userId);
        Task<NotificationResponseDto> CreateAsync(CreateNotificationRequest request);
        Task<IEnumerable<NotificationResponseDto>> CreateSystemNotificationAsync(CreateSystemNotificationRequest request);
        Task<NotificationResponseDto?> UpdateAsync(Guid id, UpdateNotificationRequest request);
        Task<NotificationResponseDto?> UpdateForUserAsync(Guid id, UpdateNotificationRequest request, Guid userId, bool isAdmin = false);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> DeleteForUserAsync(Guid id, Guid userId, bool isAdmin = false);
        Task<int> DeleteReadByUserIdAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<bool> MarkAsReadForUserAsync(Guid id, Guid userId, bool isAdmin = false);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task<bool> MarkAllAsReadForUserAsync(Guid userId, Guid currentUserId, bool isAdmin = false);
        Task<PagedResult<NotificationListDto>> GetPagedNotificationsAsync(Guid userId, PaginationRequest request);
        Task<int> GetUnreadCountAsync(Guid userId);
    }
}
