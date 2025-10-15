using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISubscriptionsRepository
    {
        Task<Subscription?> GetActiveByUserAsync(Guid userId, CancellationToken ct);
        Task<int> CountAdsCreatedThisMonthAsync(Guid userId, CancellationToken ct);
    }
}


