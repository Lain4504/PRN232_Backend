using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using BCrypt.Net;

namespace AISAM.Services.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user != null ? MapToDto(user) : null;
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user != null ? MapToDto(user) : null;
        }

        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user != null ? MapToDto(user) : null;
        }

        public async Task<UserDto?> GetUserByEmailOrUsernameAsync(string emailOrUsername)
        {
            var user = await _userRepository.GetByEmailAsync(emailOrUsername);
            if (user == null)
            {
                user = await _userRepository.GetByUsernameAsync(emailOrUsername);
            }
            return user != null ? MapToDto(user) : null;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        public IQueryable<User> Query()
        {
            return _userRepository.Query();
        }

        public async Task<UserDto> CreateUserAsync(string? email, string? username)
        {
            var user = new User
            {
                Email = email,
                Username = username
            };

            await _userRepository.CreateAsync(user);
            return MapToDto(user);
        }

        public async Task<UserDto> RegisterUserAsync(string email, string username, string password)
        {
            // Kiểm tra email đã tồn tại
            if (await EmailExistsAsync(email))
                throw new ArgumentException("Email đã được sử dụng");

            // Kiểm tra username đã tồn tại
            if (await UsernameExistsAsync(username))
                throw new ArgumentException("Username đã được sử dụng");

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);
            return MapToDto(user);
        }

        public async Task<UserDto?> LoginUserAsync(string emailOrUsername, string password)
        {
            // Tìm user theo email hoặc username
            var user = await _userRepository.GetByEmailAsync(emailOrUsername);
            if (user == null)
            {
                user = await _userRepository.GetByUsernameAsync(emailOrUsername);
            }

            // Kiểm tra user tồn tại và password đúng
            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || 
                !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null; // Login failed
            }

            return MapToDto(user);
        }

        public async Task UpdateUserAsync(UserDto userDto)
        {
            var user = await _userRepository.GetByIdAsync(userDto.Id);
            if (user == null)
                throw new ArgumentException("User not found");

            user.Email = userDto.Email;
            user.Username = userDto.Username;

            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(int id)
        {
            await _userRepository.DeleteAsync(id);
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            return await _userRepository.ExistsAsync(id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userRepository.EmailExistsAsync(email);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _userRepository.UsernameExistsAsync(username);
        }

        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                CreatedAt = user.CreatedAt,
                SocialAccounts = user.SocialAccounts?.Select(sa => new SocialAccountDto
                {
                    Id = sa.Id,
                    Provider = sa.Provider,
                    ProviderUserId = sa.ProviderUserId,
                    IsActive = sa.IsActive,
                    ExpiresAt = sa.ExpiresAt,
                    CreatedAt = sa.CreatedAt,
                    Targets = sa.SocialTargets?.Select(st => new SocialTargetDto
                    {
                        Id = st.Id,
                        ProviderTargetId = st.ProviderTargetId,
                        Name = st.Name,
                        Type = st.Type,
                        Category = st.Category,
                        ProfilePictureUrl = st.ProfilePictureUrl,
                        IsActive = st.IsActive
                    }).ToList() ?? new List<SocialTargetDto>()
                }).ToList() ?? new List<SocialAccountDto>()
            };
        }
    }
}