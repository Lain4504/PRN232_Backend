using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;

namespace AISAM.Repositories.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AisamContext _context;

        public NotificationRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }
    }
}

