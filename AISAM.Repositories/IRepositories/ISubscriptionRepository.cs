using AISAM.Data.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AISAM.Repositories.IRepositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByIdAsync(Guid id);
        Task<Subscription?> GetActiveByUserIdAsync(Guid userId);
        Task<List<Subscription>> GetByUserIdAsync(Guid userId);
        Task<Subscription> CreateAsync(Subscription subscription);
        Task<Subscription> UpdateAsync(Subscription subscription);
        Task<List<Subscription>> GetExpiredSubscriptionsAsync();
        Task<List<Subscription>> GetPendingSubscriptionsAsync();
    }
}