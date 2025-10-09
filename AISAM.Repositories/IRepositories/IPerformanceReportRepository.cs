using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IPerformanceReportRepository
    {
        Task<PerformanceReport?> GetByPostAndDateAsync(Guid postId, DateTime reportDate);
        Task<PerformanceReport> UpsertAsync(PerformanceReport report);
        Task<IEnumerable<PerformanceReport>> GetByPostAsync(Guid postId, int days = 30);
    }
}

