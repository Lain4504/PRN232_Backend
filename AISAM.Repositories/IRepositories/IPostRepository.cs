using AISAM.Data.Model;
using AISAM.Data.Enumeration;
using AISAM.Common.Dtos;

namespace AISAM.Repositories.IRepositories
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(Guid id);
        Task<IEnumerable<Post>> GetAllAsync();
        Task<Post> CreateAsync(Post post);
        Task UpdateAsync(Post post);
        Task<PagedResult<Post>> GetPagedAsync(Guid? brandId, Guid? profileId, int page, int pageSize, bool includeDeleted = false, ContentStatusEnum? status = null);
        Task<bool> SoftDeleteAsync(Guid id);
    }
}