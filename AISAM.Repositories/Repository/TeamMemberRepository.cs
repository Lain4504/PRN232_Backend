using AISAM.Common.Dtos;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;

namespace AISAM.Repositories.Repositories
{
    public class TeamMemberRepository : ITeamMemberRepository
    {
        private readonly AisamContext _context;

        public TeamMemberRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TeamMember>> GetByTeamIdAsync(Guid teamId)
        {
            return await _context.TeamMembers
                .Include(tm => tm.User)
                .Where(tm => tm.TeamId == teamId && tm.IsActive)
                .ToListAsync();
        }

        public async Task<TeamMember?> GetByTeamAndUserAsync(Guid teamId, Guid userId)
        {
            return await _context.TeamMembers
                .Include(tm => tm.Team)
                .Include(tm => tm.User)
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);
        }

        public async Task<IEnumerable<TeamMember>> GetByVendorIdAsync(Guid vendorId)
        {
            return await _context.TeamMembers
                .Include(tm => tm.Team)
                .Include(tm => tm.User)
                .Where(tm => tm.Team.ProfileId == vendorId && tm.IsActive)
                .ToListAsync();
        }

        public async Task<PagedResult<TeamMember>> GetPagedAsync(PaginationRequest request)
        {
            var query = _context.TeamMembers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                query = query.Where(x => x.Role.Contains(request.SearchTerm));

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                if (request.SortBy.Equals("role", StringComparison.OrdinalIgnoreCase))
                    query = request.SortDescending ? query.OrderByDescending(x => x.Role) : query.OrderBy(x => x.Role);
                else
                    query = query.OrderByDescending(x => x.JoinedAt);
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<TeamMember>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        // Lấy TeamMember theo Id (primary key)
        public async Task<TeamMember?> GetByIdAsync(Guid id) =>
            await _context.TeamMembers.FirstOrDefaultAsync(tm => tm.Id == id && tm.IsActive);

        // Lấy TeamMember theo UserId
        public async Task<TeamMember?> GetByUserIdAsync(Guid userId) =>
            await _context.TeamMembers.FirstOrDefaultAsync(tm => tm.UserId == userId && tm.IsActive);

        public async Task<TeamMember?> GetByUserIdAndBrandAsync(Guid userId, Guid brandId)
        {
            return await _context.TeamMembers
                .Include(tm => tm.Team)
                    .ThenInclude(t => t.TeamBrands)
                .FirstOrDefaultAsync(tm => tm.UserId == userId && 
                                         tm.IsActive &&
                                         tm.Team.TeamBrands.Any(tb => tb.BrandId == brandId));
        }

        public async Task<List<TeamMember>> GetByUserIdWithBrandsAsync(Guid userId)
        {
            return await _context.TeamMembers
                .Include(tm => tm.Team)
                    .ThenInclude(t => t.TeamBrands)
                        .ThenInclude(tb => tb.Brand)
                .Where(tm => tm.UserId == userId && tm.IsActive)
                .ToListAsync();
        }

        public async Task<TeamMember> AddAsync(TeamMember entity)
        {
            _context.TeamMembers.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(TeamMember entity)
        {
            _context.TeamMembers.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.TeamMembers.FindAsync(id);
            if (entity == null) return false;
            _context.TeamMembers.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteByTeamIdAsync(Guid teamId)
        {
            var teamMembers = await _context.TeamMembers
                .Where(tm => tm.TeamId == teamId && tm.IsActive)
                .ToListAsync();

            if (!teamMembers.Any())
                return 0;

            // Remove all team members
            _context.TeamMembers.RemoveRange(teamMembers);
            await _context.SaveChangesAsync();
            return teamMembers.Count;
        }

        public async Task<int> SoftDeleteByTeamIdAsync(Guid teamId)
        {
            var teamMembers = await _context.TeamMembers
                .Where(tm => tm.TeamId == teamId && tm.IsActive)
                .ToListAsync();

            if (!teamMembers.Any())
                return 0;

            // Soft delete all team members by setting IsActive = false
            foreach (var member in teamMembers)
            {
                member.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return teamMembers.Count;
        }

        public async Task<int> RestoreByTeamIdAsync(Guid teamId)
        {
            var teamMembers = await _context.TeamMembers
                .Where(tm => tm.TeamId == teamId && !tm.IsActive)
                .ToListAsync();

            if (!teamMembers.Any())
                return 0;

            // Restore all team members by setting IsActive = true
            foreach (var member in teamMembers)
            {
                member.IsActive = true;
            }

            await _context.SaveChangesAsync();
            return teamMembers.Count;
        }

        public async Task<bool> TeamExistsAsync(Guid teamId) =>
            await _context.Teams.AnyAsync(x => x.Id == teamId);

        public async Task<bool> UserExistsAsync(Guid userId) =>
            await _context.Users.AnyAsync(x => x.Id == userId);

        public async Task<bool> IsUserMemberOfProfileTeamsAsync(Guid userId, Guid profileId)
        {
            return await _context.TeamMembers
                .AnyAsync(tm => tm.UserId == userId && 
                               tm.IsActive && 
                               tm.Team.ProfileId == profileId && 
                               !tm.Team.IsDeleted);
        }

        public async Task<TeamMember?> GetByUserIdAndProfileAsync(Guid userId, Guid profileId)
        {
            return await _context.TeamMembers
                .Include(tm => tm.Team)
                .FirstOrDefaultAsync(tm => tm.UserId == userId && 
                                         tm.IsActive && 
                                         tm.Team.ProfileId == profileId && 
                                         !tm.Team.IsDeleted);
        }
    }
}
