using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using BookStore.Services.IServices;

namespace BookStore.Services.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;

        public UserService(IUserRepository userRepository, IAuthService authService)
        {
            _userRepository = userRepository;
            _authService = authService;
        }

        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return _userRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
        {
            // Hash password before saving
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = await _authService.HashPasswordAsync(user.PasswordHash);
            }
            
            return await _userRepository.CreateAsync(user, cancellationToken);
        }
    }
}
