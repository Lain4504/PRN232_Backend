using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
        Task<PagedResult<UserListDto>> GetPagedUsersAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    }
}
