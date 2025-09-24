using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;

namespace AISAM.Services.Service
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

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _userRepository.GetByIdAsync(id, cancellationToken);
        }

        public Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
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

        public async Task<User> CreateUserAsync(string email, string username, CancellationToken cancellationToken = default)
        {
            var user = new User
            {
                Email = email,
                Username = username,
                IsActive = true
            };

            return await _userRepository.CreateAsync(user, cancellationToken);
        }
    }
}
