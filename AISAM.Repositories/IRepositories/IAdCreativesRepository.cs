using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAdCreativesRepository
    {
        Task<AdCreative> AddAsync(AdCreative entity, CancellationToken ct);
        Task<AdCreative?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<List<AdCreative>> ListByAccountAsync(string adAccountId, CancellationToken ct);
        Task UpdateAsync(AdCreative entity, CancellationToken ct);
        Task SoftDeleteAsync(AdCreative entity, CancellationToken ct);
    }
}


