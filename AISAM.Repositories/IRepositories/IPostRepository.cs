using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IPostRepository
    {
        Task<Post> CreateAsync(Post post);
    }
}