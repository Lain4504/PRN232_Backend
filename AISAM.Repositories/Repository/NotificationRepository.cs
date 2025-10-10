using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Repositories.IRepositories;
using AISAM.Data.Model;
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
                .Include(n => n.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
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

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task<Notification> UpdateAsync(Notification notification)
        {
            notification.CreatedAt = notification.CreatedAt; // Preserve original creation time
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            if (notification == null)
                return false;

            notification.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteReadByUserIdAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead && !n.IsDeleted)
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

        public async Task<bool> MarkAsReadAsync(Guid id)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            if (notification == null)
                return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .ToListAsync();

            if (!notifications.Any())
                return false;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<NotificationListDto>> GetPagedNotificationsAsync(Guid userId, PaginationRequest request)
        {
            var query = _context.Notifications
                .Include(n => n.User)
                .Where(n => n.UserId == userId && !n.IsDeleted);

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
                    UserEmail = n.User.Email ?? ""
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

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
        }
    }
}
