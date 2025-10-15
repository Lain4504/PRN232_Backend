using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class PerformanceReportsRepository : IPerformanceReportsRepository
    {
        private readonly AisamContext _context;

        public PerformanceReportsRepository(AisamContext context)
        {
            _context = context;
        }

        public Task UpsertAdMetricsAsync(Guid adId, DateTime reportDate, long impressions, long clicks, decimal ctr, CancellationToken ct)
        {
            // Schema links performance reports to posts, not ads. For TikTok Ads MVP we skip persistence.
            return Task.CompletedTask;
        }
    }
}


