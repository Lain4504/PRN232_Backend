using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAdCampaignsRepository
    {
        Task<AdCampaign> AddAsync(AdCampaign entity, CancellationToken ct);
        Task<AdCampaign?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<List<AdCampaign>> ListAsync(Guid? userId, Guid? brandId, CancellationToken ct);
        Task UpdateAsync(AdCampaign entity, CancellationToken ct);
        Task DeleteAsync(AdCampaign entity, CancellationToken ct);
    }
}


