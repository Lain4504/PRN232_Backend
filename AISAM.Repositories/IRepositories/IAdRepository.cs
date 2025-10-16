using AISAM.Common.Dtos;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAdRepository
    {
        Task<Ad?> GetByIdAsync(Guid id);
        Task<Ad?> GetByIdWithDetailsAsync(Guid id);
        Task<List<Ad>> GetByAdSetIdAsync(Guid adSetId);
        Task<PagedResult<Ad>> GetByCampaignIdAsync(Guid campaignId, int page = 1, int pageSize = 20);
        Task<PagedResult<Ad>> GetByBrandIdAsync(Guid brandId, int page = 1, int pageSize = 20);
        Task<List<Ad>> GetActiveAdsAsync();
        Task<Ad> CreateAsync(Ad ad);
        Task UpdateAsync(Ad ad);
        Task<bool> UpdateStatusAsync(Guid id, string status);
        Task<bool> SoftDeleteAsync(Guid id);
    }
}
