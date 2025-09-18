using BookStore.Data.Model;

namespace BookStore.Repositories.IRepositories
{
    public interface IPostRepository
    {
        Task<Post> CreateAsync(Post post);
    }
}