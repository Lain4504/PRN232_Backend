using BookStore.Common.Enumeration;
using BookStore.Data.Model;

namespace BookStore.Services.IServices
{
    public interface IPostService
    {
        Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Post> CreatePostAsync(Post post, CancellationToken cancellationToken = default);
        Task<Post?> UpdatePostAsync(Post post, CancellationToken cancellationToken = default);
        Task DeletePostAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Post>> GetAllPostsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Post>> GetPostsByCategoryAsync(PostCategoryEnum category, CancellationToken cancellationToken = default);
    }
}