using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Services.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            return _userRepository.GetByIdAsync(id);
        }

        public Task<User?> GetUserByIdAsync(Guid id)
        {
            return _userRepository.GetByIdAsync(id);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Check if email already exists
            var existingUser = await _userRepository.GetByEmailAsync(user.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Credentials are managed by Supabase Auth; no password hashing here
            
            return await _userRepository.CreateAsync(user);
        }

        public async Task<User> CreateUserAsync(string email)
        {
            var user = new User
            {
                Email = email,
            };

            return await _userRepository.CreateAsync(user);
        }

        public async Task<User> GetOrCreateUserAsync(Guid supabaseUserId, string email)
        {
            // Try to get existing user by Supabase ID
            var user = await _userRepository.GetByIdAsync(supabaseUserId);
            
            if (user == null)
            {
                // Create new user with Supabase ID
                user = new User
                {
                    Id = supabaseUserId,
                    Email = email,
                    Role = Data.Enumeration.UserRoleEnum.User,
                    CreatedAt = DateTime.UtcNow
                };
                
                user = await _userRepository.CreateAsync(user);
            }
            
            return user;
        }

        public async Task<PagedResult<UserListDto>> GetPagedUsersAsync(PaginationRequest request)
        {
            // Validate pagination parameters
            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            if (request.PageSize > 100) request.PageSize = 100; // Limit max page size

            return await _userRepository.GetPagedUsersAsync(request);
        }
    }
}
