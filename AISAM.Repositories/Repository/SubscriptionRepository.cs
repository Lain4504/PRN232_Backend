using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using AISAM.Data.Model;

namespace AISAM.Repositories.Repository
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly AisamContext _context;

        public SubscriptionRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Subscription?> GetByIdAsync(Guid id)
        {
            return await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        }

        public async Task<Subscription?> GetActiveByUserIdAsync(Guid userId)
        {
            return await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive && !s.IsDeleted);
        }

        public async Task<List<Subscription>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Subscription> CreateAsync(Subscription subscription)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<Subscription> UpdateAsync(Subscription subscription)
        {
            subscription.UpdatedAt = DateTime.UtcNow;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<List<Subscription>> GetExpiredSubscriptionsAsync()
        {
            return await _context.Subscriptions
                .Where(s => s.IsActive && s.EndDate.HasValue && s.EndDate.Value < DateTime.UtcNow.Date && !s.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetPendingSubscriptionsAsync()
        {
            return await _context.Subscriptions
                .Where(s => !s.IsActive && !s.IsDeleted)
                .ToListAsync();
        }
    }
}