using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IPerformanceReportRepository
    {
        Task<PerformanceReport?> GetByIdAsync(Guid id);
        Task<PerformanceReport?> GetByPostIdAsync(Guid postId, DateTime reportDate);
        Task<PerformanceReport?> GetByAdIdAsync(Guid adId, DateTime reportDate);
        Task<List<PerformanceReport>> GetByPostIdAsync(Guid postId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<PerformanceReport>> GetByAdIdAsync(Guid adId, DateTime? startDate = null, DateTime? endDate = null);
        Task<PerformanceReport> CreateAsync(PerformanceReport performanceReport);
        Task UpdateAsync(PerformanceReport performanceReport);
        Task<PerformanceReport> CreateOrUpdateAsync(PerformanceReport performanceReport);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<decimal> GetTotalSpendByAdAsync(Guid adId, DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTotalSpendByProfileIdAsync(Guid profileId, DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTotalSpendByUserIdAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
