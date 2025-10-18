using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AisamContext _context;

        public NotificationRepository(AisamContext context)
        {
            _context = context;
        }
        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await _context.Notifications
                .Include(n => n.Profile)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        }
    
        public async Task<Notification> CreateAsync(Notification notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;
            notification.IsDeleted = false;

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

       
        public async Task<bool> DeleteAsync(Guid id)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            if (notification == null)
                return false;

            notification.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsReadAsync(Guid id)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            if (notification == null)
                return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> MarkAsReadBulkAsync(IEnumerable<Guid> ids, Guid profileId)
        {
            var notifications = await _context.Notifications
                .Where(n => ids.Contains(n.Id) && n.ProfileId == profileId && !n.IsDeleted && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return notifications.Count;
        }

        public async Task<int> MarkAllAsReadAsync(Guid profileId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.ProfileId == profileId && !n.IsDeleted && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return notifications.Count;
        }

        public async Task<PagedResult<NotificationListDto>> GetPagedNotificationsAsync(Guid profileId, PaginationRequest request, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Include(n => n.Profile)
                .Where(n => n.ProfileId == profileId && !n.IsDeleted);

            // Apply unread filter at database level
            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(n => n.Title.Contains(request.SearchTerm) || n.Message.Contains(request.SearchTerm));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "title" => request.SortDescending ? query.OrderByDescending(n => n.Title) : query.OrderBy(n => n.Title),
                "createdat" => request.SortDescending ? query.OrderByDescending(n => n.CreatedAt) : query.OrderBy(n => n.CreatedAt),
                "isread" => request.SortDescending ? query.OrderByDescending(n => n.IsRead) : query.OrderBy(n => n.IsRead),
                _ => query.OrderByDescending(n => n.CreatedAt)
            };

            // Apply pagination
            var notifications = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(n => new NotificationListDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    UserEmail = n.Profile.Name ?? ""
                })
                .ToListAsync();

            return new PagedResult<NotificationListDto>
            {
                Data = notifications,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<int> GetUnreadCountAsync(Guid profileId)
        {
            return await _context.Notifications
                .CountAsync(n => n.ProfileId == profileId && !n.IsRead && !n.IsDeleted);
        }


        public async Task<int> DeleteOldNotificationsAsync(int daysOld = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var notifications = await _context.Notifications
                .Where(n => n.CreatedAt < cutoffDate && !n.IsDeleted)
                .ToListAsync();

            if (!notifications.Any())
                return 0;

            foreach (var notification in notifications)
            {
                notification.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
            return notifications.Count;
        }
    }
}