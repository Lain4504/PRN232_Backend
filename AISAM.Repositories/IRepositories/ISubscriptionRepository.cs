using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByIdAsync(Guid id);
        Task<Subscription?> GetActiveByUserIdAsync(Guid userId);
        Task<List<Subscription>> GetByUserIdAsync(Guid userId);
        Task<Subscription> CreateAsync(Subscription subscription);
        Task UpdateAsync(Subscription subscription);
        Task<bool> SoftDeleteAsync(Guid id);
    }
}
