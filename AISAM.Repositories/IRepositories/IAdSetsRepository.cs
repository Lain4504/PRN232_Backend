using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAdSetsRepository
    {
        Task<AdSet> AddAsync(AdSet entity, CancellationToken ct);
        Task<AdSet?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<List<AdSet>> ListByCampaignAsync(Guid campaignId, CancellationToken ct);
        Task UpdateAsync(AdSet entity, CancellationToken ct);
        Task SoftDeleteAsync(AdSet entity, CancellationToken ct);
    }
}


