using BookStore.Data.Model;

namespace BookStore.Repositories.IRepositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}
