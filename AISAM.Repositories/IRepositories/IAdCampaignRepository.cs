using AISAM.Common.Dtos;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAdCampaignRepository
    {
        Task<AdCampaign?> GetByIdAsync(Guid id);
        Task<AdCampaign?> GetByIdWithDetailsAsync(Guid id);
        Task<PagedResult<AdCampaign>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<PagedResult<AdCampaign>> GetByBrandIdAsync(Guid brandId, int page = 1, int pageSize = 20);
        Task<AdCampaign> CreateAsync(AdCampaign adCampaign);
        Task UpdateAsync(AdCampaign adCampaign);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<int> CountActiveByUserIdAsync(Guid userId);
        Task<int> CountActiveByBrandIdAsync(Guid brandId);
    }
}
