using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAdSetRepository
    {
        Task<AdSet?> GetByIdAsync(Guid id);
        Task<AdSet?> GetByIdWithDetailsAsync(Guid id);
        Task<List<AdSet>> GetByCampaignIdAsync(Guid campaignId);
        Task<AdSet> CreateAsync(AdSet adSet);
        Task UpdateAsync(AdSet adSet);
        Task<bool> SoftDeleteAsync(Guid id);
    }
}
