using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);
        Task<User> CreateUserAsync(string email, CancellationToken cancellationToken = default);
        Task<User> GetOrCreateUserAsync(Guid supabaseUserId, string email, CancellationToken cancellationToken = default);
        Task<PagedResult<UserListDto>> GetPagedUsersAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    }
}
