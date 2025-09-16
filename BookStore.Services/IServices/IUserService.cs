using BookStore.Data.Model;

namespace BookStore.Services
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}
