using AISAM.Data.Enumeration;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(Guid id);
        Task<Notification> CreateAsync(Notification notification);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<int> MarkAsReadBulkAsync(IEnumerable<Guid> ids, Guid profileId);
        Task<PagedResult<NotificationListDto>> GetPagedNotificationsAsync(Guid profileId, PaginationRequest request, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync(Guid profileId);
        Task<bool> DeleteAsync(Guid id);
        Task<int> DeleteOldNotificationsAsync(int daysOld = 30);
    }
}
