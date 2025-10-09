using AISAM.Common.Dtos;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IPostRepository
    {
        Task<PagedResult<Post>> GetPagedByUserIdAsync(Guid userId, PaginationRequest request);
        Task<Post?> GetByIdAsync(Guid id);
        Task<IEnumerable<Post>> GetAllAsync();
        Task<Post> CreateAsync(Post post);
        Task<Post> UpdateAsync(Post post);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
        // Methods for tracking published posts
        Task<IEnumerable<Post>> GetPostsByContentIdAsync(Guid contentId);
        Task<IEnumerable<Post>> GetPostsByIntegrationIdAsync(Guid integrationId);
        Task<Post?> GetByExternalPostIdAsync(string externalPostId);
        Task<IEnumerable<Post>> GetPublishedPostsAsync();
        Task<IEnumerable<Post>> GetPostsPublishedBetweenAsync(DateTime startDate, DateTime endDate);
    }
}