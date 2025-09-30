using AISAM.Data.Enumeration;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IContentRepository
    {
        Task<Content?> GetByIdAsync(Guid id);
        Task<Content?> GetByIdIncludingDeletedAsync(Guid id);
        Task<IEnumerable<Content>> GetByBrandIdAsync(Guid brandId);
        
        Task<(IEnumerable<Content> Items, int TotalCount)> GetByBrandIdPagedAsync(
            Guid brandId,
            int page,
            int pageSize,
            string? searchTerm,
            string? sortBy,
            bool sortDescending,
            AdTypeEnum? adType,
            bool onlyDeleted,
            ContentStatusEnum? status);
        Task<Content> CreateAsync(Content content);
        Task UpdateAsync(Content content);
        Task DeleteAsync(Guid id);
        Task HardDeleteAsync(Guid id);
        Task RestoreAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
