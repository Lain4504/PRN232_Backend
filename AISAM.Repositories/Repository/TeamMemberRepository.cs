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

        public async Task<TeamMember?> GetByIdAsync(Guid id) =>
            await _context.TeamMembers.FindAsync(id);

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

        public async Task<bool> TeamExistsAsync(Guid teamId) =>
            await _context.Teams.AnyAsync(x => x.Id == teamId);

        public async Task<bool> UserExistsAsync(Guid userId) =>
            await _context.Users.AnyAsync(x => x.Id == userId);
    }
}
