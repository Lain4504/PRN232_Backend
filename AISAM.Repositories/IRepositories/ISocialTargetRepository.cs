using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISocialTargetRepository
    {
        Task<SocialTarget?> GetByIdAsync(Guid id);
        Task<SocialTarget?> GetByProviderTargetIdAsync(Guid socialAccountId, string providerTargetId);
        Task<SocialTarget?> GetByProviderTargetIdAsync(string providerTargetId);
        Task<IEnumerable<SocialTarget>> GetBySocialAccountIdAsync(Guid socialAccountId);
        Task<IEnumerable<SocialTarget>> GetByUserIdAsync(Guid userId);
        Task<SocialTarget> CreateAsync(SocialTarget target);
        Task UpdateAsync(SocialTarget target);
        Task DeleteAsync(Guid id);
        Task DeleteBySocialAccountIdAsync(Guid socialAccountId);
        Task<bool> ExistsAsync(Guid socialAccountId, string providerTargetId);
    }
}