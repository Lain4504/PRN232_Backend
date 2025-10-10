using AISAM.Data.Enumeration;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<Notification?> GetByIdAsync(Guid id);
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId);
        Task<Notification> CreateAsync(Notification notification);
        Task<Notification> UpdateAsync(Notification notification);
        Task<bool> DeleteAsync(Guid id);
        Task<int> DeleteReadByUserIdAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task<PagedResult<NotificationListDto>> GetPagedNotificationsAsync(Guid userId, PaginationRequest request);
        Task<int> GetUnreadCountAsync(Guid userId);
    }
}
