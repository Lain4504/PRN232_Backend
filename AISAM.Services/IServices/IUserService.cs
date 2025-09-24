using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface IUserService
    {
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<UserDto?> GetUserByUsernameAsync(string username);
        Task<UserDto?> GetUserByEmailOrUsernameAsync(string emailOrUsername);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto> CreateUserAsync(string? email, string? username);
        Task<UserDto> RegisterUserAsync(string email, string username, string password);
        Task<UserDto?> LoginUserAsync(string emailOrUsername, string password);
        Task UpdateUserAsync(UserDto userDto);
        Task DeleteUserAsync(int id);
        Task<bool> UserExistsAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
    }
}
