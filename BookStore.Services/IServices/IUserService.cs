using BookStore.Data.Model;

namespace BookStore.Services
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    }
}
