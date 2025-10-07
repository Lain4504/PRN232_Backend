using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
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

        public async Task<IEnumerable<TeamMember>> GetByUserIdAsync(Guid userId)
        {
            return await _context.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm => tm.UserId == userId && tm.IsActive)
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
                .Where(tm => tm.Team.VendorId == vendorId && tm.IsActive)
                .ToListAsync();
        }
    }
}


