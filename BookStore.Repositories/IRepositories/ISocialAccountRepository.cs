using BookStore.Data.Model;

namespace BookStore.Repositories.IRepositories
{
    public interface ISocialAccountRepository
    {
        Task<SocialAccount?> GetByIdAsync(int id);
        Task<SocialAccount?> GetByIdWithTargetsAsync(int id);
        Task<SocialAccount?> GetByProviderAndUserIdAsync(string provider, string providerUserId);
        Task<SocialAccount?> GetByUserIdAndProviderAsync(int userId, string provider);
        Task<IEnumerable<SocialAccount>> GetByUserIdAsync(int userId);
        Task<IEnumerable<SocialAccount>> GetByProviderAsync(string provider);
        Task<SocialAccount> CreateAsync(SocialAccount account);
        Task UpdateAsync(SocialAccount account);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(string provider, string providerUserId);
    }
}