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

        public async Task<PerformanceReport?> GetByPostAndDateAsync(Guid postId, DateTime reportDate)
        {
            return await _context.PerformanceReports
                .FirstOrDefaultAsync(r => r.PostId == postId && r.ReportDate == reportDate.Date);
        }

        public async Task<PerformanceReport> UpsertAsync(PerformanceReport report)
        {
            var existing = await GetByPostAndDateAsync(report.PostId, report.ReportDate);
            if (existing != null)
            {
                existing.Impressions = report.Impressions;
                existing.Engagement = report.Engagement;
                existing.Ctr = report.Ctr;
                existing.EstimatedRevenue = report.EstimatedRevenue;
                existing.RawData = report.RawData;
                _context.PerformanceReports.Update(existing);
                await _context.SaveChangesAsync();
                return existing;
            }

            _context.PerformanceReports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<IEnumerable<PerformanceReport>> GetByPostAsync(Guid postId, int days = 30)
        {
            var since = DateTime.UtcNow.Date.AddDays(-days);
            return await _context.PerformanceReports
                .Where(r => r.PostId == postId && r.ReportDate >= since)
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();
        }
    }
}

