using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly AisamContext _context;

        public UserRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task<PagedResult<UserListDto>> GetPagedUsersAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsNoTracking();

            // Apply search filter
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(u => u.Email!.Contains(request.SearchTerm));
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "email" => request.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "createdat" => request.SortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            // Apply pagination
            var users = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UserListDto
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    CreatedAt = u.CreatedAt,
                    SocialAccountsCount = u.SocialAccounts!.Count
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<UserListDto>
            {
                Data = users,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
