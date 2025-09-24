using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISocialAccountRepository
    {
        Task<SocialAccount?> GetByIdAsync(Guid id);
        Task<SocialAccount?> GetByIdWithTargetsAsync(Guid id);
        Task<SocialAccount?> GetByProviderAndUserIdAsync(string provider, string providerUserId);
        Task<SocialAccount?> GetByUserIdAndProviderAsync(Guid userId, string provider);
        Task<IEnumerable<SocialAccount>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<SocialAccount>> GetByProviderAsync(string provider);
        Task<SocialAccount> CreateAsync(SocialAccount account);
        Task UpdateAsync(SocialAccount account);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(string provider, string providerUserId);
    }
}