using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User> CreateUserAsync(Guid supabaseUserId, string email);
        Task<PagedResult<UserListDto>> GetPagedUsersAsync(PaginationRequest request);
        /**
         * Create user internal 
         */
        Task<User> CreateUserInternalAsync();
    }
}
