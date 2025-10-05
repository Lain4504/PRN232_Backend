using AISAM.Data.Enumeration;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISocialAccountRepository
    {
        Task<SocialAccount?> GetByIdAsync(Guid id);
        Task<SocialAccount?> GetByIdWithIntegrationsAsync(Guid id);
        Task<SocialAccount?> GetByPlatformAndAccountIdAsync(SocialPlatformEnum platform, string accountId);
        Task<SocialAccount?> GetByUserIdPlatformAndAccountIdAsync(Guid userId, SocialPlatformEnum platform, string accountId);
        Task<SocialAccount?> GetByUserIdAndPlatformAsync(Guid userId, SocialPlatformEnum platform);
        Task<IEnumerable<SocialAccount>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<SocialAccount>> GetByPlatformAsync(SocialPlatformEnum platform);
        Task<SocialAccount> CreateAsync(SocialAccount account);
        Task UpdateAsync(SocialAccount account);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(SocialPlatformEnum platform, string accountId);
    }
}