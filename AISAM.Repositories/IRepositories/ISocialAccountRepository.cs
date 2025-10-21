using AISAM.Data.Enumeration;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISocialAccountRepository
    {
        Task<SocialAccount?> GetByIdAsync(Guid id);
        Task<SocialAccount?> GetByIdWithIntegrationsAsync(Guid id);
        Task<SocialAccount?> GetByPlatformAndAccountIdAsync(SocialPlatformEnum platform, string accountId);
        Task<SocialAccount?> GetByProfileIdPlatformAndAccountIdAsync(Guid profileId, SocialPlatformEnum platform, string accountId);
        Task<SocialAccount?> GetByProfileIdAndPlatformAsync(Guid profileId, SocialPlatformEnum platform);
        Task<IEnumerable<SocialAccount>> GetByProfileIdAsync(Guid profileId);
        Task<IEnumerable<SocialAccount>> GetByPlatformAsync(SocialPlatformEnum platform);
        Task<SocialAccount> CreateAsync(SocialAccount account);
        Task UpdateAsync(SocialAccount account);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(SocialPlatformEnum platform, string accountId);
    }
}