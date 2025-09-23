using BookStore.Common.Enumeration;
using BookStore.Data.Model;

namespace BookStore.Repositories.IRepositories
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Post> CreateAsync(Post post, CancellationToken cancellationToken = default);
        Task<Post> UpdateAsync(Post post, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Post>> GetAllPostsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Post>> GetPostsByCategoryAsync(PostCategoryEnum category, CancellationToken cancellationToken = default);
    }
}