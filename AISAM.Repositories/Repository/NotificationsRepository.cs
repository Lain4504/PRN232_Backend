using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;

namespace AISAM.Repositories.Repository
{
    public class NotificationsRepository : INotificationsRepository
    {
        private readonly AisamContext _context;

        public NotificationsRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Notification> AddAsync(Notification entity, CancellationToken ct)
        {
            _context.Notifications.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }
    }
}


