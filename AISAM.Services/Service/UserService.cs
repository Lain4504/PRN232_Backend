using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
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

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return null;
            }

            return user;
        }

        public async Task<User> CreateUserAsync(Guid supabaseUserId, string email)
        {
            // Strict create: if the user already exists, throw to surface duplicate calls
            var existingUser = await _userRepository.GetByIdAsync(supabaseUserId);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User already exists");
            }

            var newUser = new User
            {
                Id = supabaseUserId,
                Email = email,
                Role = Data.Enumeration.UserRoleEnum.User,
                CreatedAt = DateTime.UtcNow
            };

            return await _userRepository.CreateAsync(newUser);
        }

        public async Task<PagedResult<UserListDto>> GetPagedUsersAsync(PaginationRequest request)
        {
            // Validate pagination parameters
            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            if (request.PageSize > 100) request.PageSize = 100; // Limit max page size

            return await _userRepository.GetPagedUsersAsync(request);
        }

        public async Task<User> CreateUserInternalAsync()
        {
            const string internalEmail = "admin@aisam.com";
            var existingUser = await _userRepository.GetByEmailAsync(internalEmail);
            
            if (existingUser != null)
            {
                return existingUser;
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = internalEmail,
                Role = Data.Enumeration.UserRoleEnum.Admin,
                CreatedAt = DateTime.UtcNow
            };

            return await _userRepository.CreateAsync(newUser);
        }
    }
}
