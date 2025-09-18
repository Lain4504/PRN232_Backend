using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISocialTargetRepository
    {
        Task<SocialTarget?> GetByIdAsync(int id);
        Task<SocialTarget?> GetByProviderTargetIdAsync(int socialAccountId, string providerTargetId);
        Task<SocialTarget?> GetByProviderTargetIdAsync(string providerTargetId);
        Task<IEnumerable<SocialTarget>> GetBySocialAccountIdAsync(int socialAccountId);
        Task<IEnumerable<SocialTarget>> GetByUserIdAsync(int userId);
        Task<SocialTarget> CreateAsync(SocialTarget target);
        Task UpdateAsync(SocialTarget target);
        Task DeleteAsync(int id);
        Task DeleteBySocialAccountIdAsync(int socialAccountId);
        Task<bool> ExistsAsync(int socialAccountId, string providerTargetId);
    }
}