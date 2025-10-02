using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User> CreateUserAsync(User user);
        Task<User> CreateUserAsync(string email);
        Task<User> GetOrCreateUserAsync(Guid supabaseUserId, string email);
        Task<PagedResult<UserListDto>> GetPagedUsersAsync(PaginationRequest request);
    }
}
