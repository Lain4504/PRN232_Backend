using AISAM.Data.Enumeration;
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

        public async Task<Subscription?> GetActiveByUserIdAsync(Guid userId)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId == userId && s.IsActive && !s.IsDeleted)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountApprovedOrPublishedThisMonthAsync(Guid userId)
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            return await _context.Contents
                .Include(c => c.Brand)
                .Where(c => c.Brand.UserId == userId && !c.IsDeleted &&
                            (c.Status == ContentStatusEnum.Approved || c.Status == ContentStatusEnum.Published) &&
                            c.CreatedAt >= startOfMonth)
                .CountAsync();
        }
    }
}


