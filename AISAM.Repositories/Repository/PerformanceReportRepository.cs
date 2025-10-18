using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class PerformanceReportRepository : IPerformanceReportRepository
    {
        private readonly AisamContext _context;

        public PerformanceReportRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<PerformanceReport?> GetByIdAsync(Guid id)
        {
            return await _context.PerformanceReports
                .Include(pr => pr.Post)
                .Include(pr => pr.Ad)
                .FirstOrDefaultAsync(pr => pr.Id == id && !pr.IsDeleted);
        }

        public async Task<PerformanceReport?> GetByPostIdAsync(Guid postId, DateTime reportDate)
        {
            return await _context.PerformanceReports
                .FirstOrDefaultAsync(pr => pr.PostId == postId && 
                                         pr.ReportDate.Date == reportDate.Date && 
                                         !pr.IsDeleted);
        }

        public async Task<PerformanceReport?> GetByAdIdAsync(Guid adId, DateTime reportDate)
        {
            return await _context.PerformanceReports
                .FirstOrDefaultAsync(pr => pr.AdId == adId && 
                                         pr.ReportDate.Date == reportDate.Date && 
                                         !pr.IsDeleted);
        }

        public async Task<List<PerformanceReport>> GetByPostIdAsync(Guid postId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.PerformanceReports
                .Where(pr => pr.PostId == postId && !pr.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(pr => pr.ReportDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(pr => pr.ReportDate <= endDate.Value);

            return await query
                .OrderBy(pr => pr.ReportDate)
                .ToListAsync();
        }

        public async Task<List<PerformanceReport>> GetByAdIdAsync(Guid adId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.PerformanceReports
                .Where(pr => pr.AdId == adId && !pr.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(pr => pr.ReportDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(pr => pr.ReportDate <= endDate.Value);

            return await query
                .OrderBy(pr => pr.ReportDate)
                .ToListAsync();
        }

        public async Task<PerformanceReport> CreateAsync(PerformanceReport performanceReport)
        {
            _context.PerformanceReports.Add(performanceReport);
            await _context.SaveChangesAsync();
            return performanceReport;
        }

        public async Task UpdateAsync(PerformanceReport performanceReport)
        {
            _context.PerformanceReports.Update(performanceReport);
            await _context.SaveChangesAsync();
        }

        public async Task<PerformanceReport> CreateOrUpdateAsync(PerformanceReport performanceReport)
        {
            PerformanceReport? existing = null;

            if (performanceReport.PostId.HasValue)
            {
                existing = await GetByPostIdAsync(performanceReport.PostId.Value, performanceReport.ReportDate);
            }
            else if (performanceReport.AdId.HasValue)
            {
                existing = await GetByAdIdAsync(performanceReport.AdId.Value, performanceReport.ReportDate);
            }

            if (existing != null)
            {
                existing.Impressions = performanceReport.Impressions;
                existing.Engagement = performanceReport.Engagement;
                existing.Ctr = performanceReport.Ctr;
                existing.EstimatedRevenue = performanceReport.EstimatedRevenue;
                existing.RawData = performanceReport.RawData;
                await UpdateAsync(existing);
                return existing;
            }
            else
            {
                return await CreateAsync(performanceReport);
            }
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var performanceReport = await _context.PerformanceReports.FindAsync(id);
            if (performanceReport == null) return false;

            performanceReport.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotalSpendByAdAsync(Guid adId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.PerformanceReports
                .Where(pr => pr.AdId == adId && !pr.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(pr => pr.ReportDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(pr => pr.ReportDate <= endDate.Value);

            return await query.SumAsync(pr => pr.EstimatedRevenue);
        }

        public async Task<decimal> GetTotalSpendByProfileIdAsync(Guid profileId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.PerformanceReports
                .Include(pr => pr.Ad)
                    .ThenInclude(a => a.AdSet)
                        .ThenInclude(ads => ads.Campaign)
                            .ThenInclude(c => c.Brand)
                .Where(pr => pr.Ad != null && 
                           pr.Ad.AdSet.Campaign.Brand.ProfileId == profileId && 
                           !pr.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(pr => pr.ReportDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(pr => pr.ReportDate <= endDate.Value);

            return await query.SumAsync(pr => pr.EstimatedRevenue);
        }

        public async Task<decimal> GetTotalSpendByUserIdAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.PerformanceReports
                .Include(pr => pr.Ad)
                    .ThenInclude(a => a.AdSet)
                        .ThenInclude(ads => ads.Campaign)
                            .ThenInclude(c => c.Brand)
                                .ThenInclude(b => b.Profile)
                .Where(pr => pr.Ad != null && 
                           pr.Ad.AdSet.Campaign.Brand.Profile.UserId == userId && 
                           !pr.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(pr => pr.ReportDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(pr => pr.ReportDate <= endDate.Value);

            return await query.SumAsync(pr => pr.EstimatedRevenue);
        }
    }
}
