using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Common.Models;

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
            // Check if email already exists
            var existingUser = await _userRepository.GetByEmailAsync(user.Email, cancellationToken);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Hash password before saving
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = await _authService.HashPasswordAsync(user.PasswordHash);
            }
            
            return await _userRepository.CreateAsync(user, cancellationToken);
        }

        public async Task<User> CreateUserAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = new User
            {
                Email = email,
                IsActive = true
            };

            return await _userRepository.CreateAsync(user, cancellationToken);
        }

        public async Task<PagedResult<UserListDto>> GetPagedUsersAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            // Validate pagination parameters
            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            if (request.PageSize > 100) request.PageSize = 100; // Limit max page size

            return await _userRepository.GetPagedUsersAsync(request, cancellationToken);
        }
    }
}
