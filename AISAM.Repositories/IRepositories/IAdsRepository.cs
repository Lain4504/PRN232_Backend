using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAdsRepository
    {
        Task<Ad> AddAsync(Ad entity, CancellationToken ct);
        Task<Ad?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<List<Ad>> ListAsync(Guid? userId, Guid? brandId, Guid? campaignId, CancellationToken ct);
        Task UpdateAsync(Ad entity, CancellationToken ct);
        Task SoftDeleteAsync(Ad entity, CancellationToken ct);
    }
}


