using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAdCreativeRepository
    {
        Task<AdCreative?> GetByIdAsync(Guid id);
        Task<AdCreative?> GetByIdWithDetailsAsync(Guid id);
        Task<AdCreative?> GetByContentIdAsync(Guid contentId);
        Task<AdCreative> CreateAsync(AdCreative adCreative);
        Task UpdateAsync(AdCreative adCreative);
        Task<bool> SoftDeleteAsync(Guid id);
    }
}
