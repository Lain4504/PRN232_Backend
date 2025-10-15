using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IPerformanceReportsRepository
    {
        Task UpsertAdMetricsAsync(Guid adId, DateTime reportDate, long impressions, long clicks, decimal ctr, CancellationToken ct);
    }
}


