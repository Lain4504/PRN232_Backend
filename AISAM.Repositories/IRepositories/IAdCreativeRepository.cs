using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAdCreativeRepository
    {
        Task<AdCreative?> GetByIdAsync(Guid id);
        Task<AdCreative?> GetByIdWithDetailsAsync(Guid id);
        Task<AdCreative?> GetByContentIdAsync(Guid contentId);
        Task<(IEnumerable<AdCreative> Data, int TotalCount)> GetByAdSetIdPagedAsync(Guid adSetId, int page, int pageSize, string? search = null, string? type = null, string? sortBy = null, string? sortOrder = null);
        Task<AdCreative> CreateAsync(AdCreative adCreative);
        Task UpdateAsync(AdCreative adCreative);
        Task<bool> SoftDeleteAsync(Guid id);
    }
}
