using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repositories
{
    public class TeamRepository : ITeamRepository
    {
        private readonly AisamContext _context;

        public TeamRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Team> CreateAsync(Team team)
        {
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task<Team?> GetByIdAsync(Guid id)
        {
            return await _context.Teams
                .Include(t => t.Profile)
                .Include(t => t.TeamMembers)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Team>> GetByProfileIdAsync(Guid profileId, Guid userId)
        {
            return await _context.Teams
                .Include(t => t.Profile)
                .Include(t => t.TeamMembers)
                .Where(t => t.ProfileId == profileId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ExistsByNameAndProfileAsync(string name, Guid profileId)
        {
            return await _context.Teams
                .AnyAsync(t => t.Name.ToLower() == name.ToLower() 
                          && t.ProfileId == profileId 
                          && !t.IsDeleted);
        }

        public async Task<Team> UpdateAsync(Team team)
        {
            _context.Teams.Update(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task DeleteAsync(Guid id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team != null)
            {
                team.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}