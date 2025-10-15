using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class SubscriptionsRepository : ISubscriptionsRepository
    {
        private readonly AisamContext _context;

        public SubscriptionsRepository(AisamContext context)
        {
            _context = context;
        }

        public Task<AISAM.Data.Model.Subscription?> GetActiveByUserAsync(Guid userId, CancellationToken ct)
        {
            return _context.Subscriptions.AsNoTracking()
                .Where(s => s.UserId == userId && s.IsActive && !s.IsDeleted)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync(ct);
        }

        public Task<int> CountAdsCreatedThisMonthAsync(Guid userId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var start = new DateTime(now.Year, now.Month, 1);
            return _context.Ads
                .Include(a => a.AdSet)
                .ThenInclude(s => s.Campaign)
                .Where(a => a.AdSet.Campaign.UserId == userId && a.CreatedAt >= start)
                .CountAsync(ct);
        }
    }
}


