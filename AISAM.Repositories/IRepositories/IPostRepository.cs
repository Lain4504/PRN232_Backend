using BookStore.Data.Model;

namespace BookStore.Repositories.IRepositories
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(int id);
        Task<Post?> GetByProviderPostIdAsync(string providerPostId);
        Task<IEnumerable<Post>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Post>> GetBySocialAccountIdAsync(int socialAccountId);
        Task<IEnumerable<Post>> GetBySocialTargetIdAsync(int socialTargetId);
        Task<IEnumerable<Post>> GetScheduledPostsAsync();
        Task<IEnumerable<Post>> GetByStatusAsync(PostStatus status);
        Task<Post> CreateAsync(Post post);
        Task UpdateAsync(Post post);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}