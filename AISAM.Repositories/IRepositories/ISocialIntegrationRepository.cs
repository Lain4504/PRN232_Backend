using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISocialIntegrationRepository
    {
        Task<SocialIntegration?> GetByIdAsync(Guid id);
        Task<SocialIntegration?> GetByExternalIdAsync(string externalId);
        Task<IEnumerable<SocialIntegration>> GetBySocialAccountIdAsync(Guid socialAccountId);
        Task<IEnumerable<SocialIntegration>> GetByUserIdAsync(Guid userId);
        Task<SocialIntegration?> GetByBrandIdAsync(Guid brandId);
        Task<IEnumerable<SocialIntegration>> GetByProfileIdAsync(Guid profileId);
        Task<SocialIntegration> CreateAsync(SocialIntegration integration);
        Task UpdateAsync(SocialIntegration integration);
        Task DeleteAsync(Guid id);
        Task DeleteBySocialAccountIdAsync(Guid socialAccountId);
        Task<bool> ExistsAsync(Guid socialAccountId, string externalId);
    }
}
