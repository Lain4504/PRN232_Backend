using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

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
                .Include(s => s.Profile)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        }

        public async Task<Subscription?> GetActiveByProfileIdAsync(Guid profileId)
        {
            var now = DateTime.UtcNow.Date;
            return await _context.Subscriptions
                .Include(s => s.Profile)
                .FirstOrDefaultAsync(s => s.ProfileId == profileId && 
                                        s.IsActive && 
                                        !s.IsDeleted &&
                                        s.StartDate <= now &&
                                        (s.EndDate == null || s.EndDate >= now));
        }

        public async Task<Subscription?> GetActiveByUserIdAsync(Guid userId)
        {
            var now = DateTime.UtcNow.Date;
            return await _context.Subscriptions
                .Include(s => s.Profile)
                .Where(s => s.Profile.UserId == userId && 
                           s.IsActive && 
                           !s.IsDeleted &&
                           s.StartDate <= now &&
                           (s.EndDate == null || s.EndDate >= now))
                .FirstOrDefaultAsync();
        }

        public async Task<List<Subscription>> GetByProfileIdAsync(Guid profileId)
        {
            return await _context.Subscriptions
                .Include(s => s.Profile)
                .Where(s => s.ProfileId == profileId && !s.IsDeleted)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Subscription> CreateAsync(Subscription subscription)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task UpdateAsync(Subscription subscription)
        {
            subscription.UpdatedAt = DateTime.UtcNow;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null) return false;

            subscription.IsDeleted = true;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
