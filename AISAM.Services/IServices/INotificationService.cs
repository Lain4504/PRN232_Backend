using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface INotificationService
    {
        Task<NotificationResponseDto?> GetByIdAsync(Guid id);
        Task<PagedResult<NotificationListDto>> GetPagedNotificationsAsync(Guid userId, PaginationRequest request, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<int> MarkAsReadBulkAsync(IEnumerable<Guid> ids, Guid userId);
        Task<NotificationResponseDto> CreateAsync(CreateNotificationRequest request);
        Task<bool> DeleteAsync(Guid id, Guid userId);
        Task<int> DeleteOldNotificationsAsync(int daysOld = 30);
    }
}
