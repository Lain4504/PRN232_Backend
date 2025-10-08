using AISAM.Common.Dtos;
using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class TeamRepository : ITeamRepository
    {
        private readonly AisamContext _context;

        public TeamRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Team?> GetByIdAsync(Guid id)
        {
            return await _context.Teams
                .Include(t => t.TeamMembers)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }

        public async Task<Team?> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _context.Teams
                .Include(t => t.TeamMembers)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<PagedResult<Team>> GetPagedForAdminAsync(PaginationRequest request)
        {
            var query = _context.Teams.Where(t => !t.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var s = request.SearchTerm.ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                    "createdat" => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                    _ => query.OrderBy(t => t.Name)
                };
            }
            else
            {
                query = query.OrderBy(t => t.Name);
            }

            var total = await query.CountAsync();
            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Team>
            {
                Data = data,
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PagedResult<Team>> GetPagedForVendorAsync(Guid vendorId, PaginationRequest request)
        {
            var query = _context.Teams.Where(t => t.VendorId == vendorId && !t.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var s = request.SearchTerm.ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                    "createdat" => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                    _ => query.OrderBy(t => t.Name)
                };
            }
            else
            {
                query = query.OrderBy(t => t.Name);
            }

            var total = await query.CountAsync();
            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Team>
            {
                Data = data,
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PagedResult<Team>> GetPagedForMemberAsync(Guid userId, PaginationRequest request)
        {
            var query = _context.Teams
                .Where(t => !t.IsDeleted && _context.TeamMembers.Any(tm => tm.TeamId == t.Id && tm.UserId == userId));

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var s = request.SearchTerm.ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                    "createdat" => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                    _ => query.OrderBy(t => t.Name)
                };
            }
            else
            {
                query = query.OrderBy(t => t.Name);
            }

            var total = await query.CountAsync();
            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Team>
            {
                Data = data,
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<Team> AddAsync(Team team)
        {
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task UpdateAsync(Team team)
        {
            _context.Teams.Update(team);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UserExistsAsync(Guid userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<bool> IsMemberAsync(Guid teamId, Guid userId)
        {
            return await _context.TeamMembers.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);
        }

        public async Task<string?> GetMemberPermissionsJsonAsync(Guid teamId, Guid userId)
        {
            var tm = await _context.TeamMembers.AsNoTracking()
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);
            return tm?.Permissions;
        }
    }
}
