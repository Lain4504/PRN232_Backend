using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByIdAsync(Guid id);
        Task<Subscription?> GetActiveByProfileIdAsync(Guid profileId);
        Task<List<Subscription>> GetByProfileIdAsync(Guid profileId);
        Task<List<Subscription>> GetByUserIdAsync(Guid userId);
        Task<List<Subscription>> GetAllSubscriptionsAsync();
        Task<Subscription?> GetByPayOSOrderCodeAsync(string orderCode);
        Task<Subscription> CreateAsync(Subscription subscription);
        Task UpdateAsync(Subscription subscription);
        Task<bool> SoftDeleteAsync(Guid id);
    }
}
