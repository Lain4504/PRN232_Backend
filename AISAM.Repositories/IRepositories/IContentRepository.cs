using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IContentRepository
    {
        Task<Content?> GetByIdAsync(Guid id);
        Task<IEnumerable<Content>> GetByBrandIdAsync(Guid brandId);
        Task<IEnumerable<Content>> GetByUserIdAsync(Guid userId);
        Task<Content> CreateAsync(Content content);
        Task UpdateAsync(Content content);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
