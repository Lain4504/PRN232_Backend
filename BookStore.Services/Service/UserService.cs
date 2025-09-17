using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;

namespace BookStore.Services.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return _userRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
        {
            return await _userRepository.CreateAsync(user, cancellationToken);
        }
    }
}
