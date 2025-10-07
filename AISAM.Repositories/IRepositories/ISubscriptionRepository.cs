using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetActiveByUserIdAsync(Guid userId);
        Task<int> CountApprovedOrPublishedThisMonthAsync(Guid userId);
    }
}


