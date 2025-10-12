using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class TeamBrandRepository : ITeamBrandRepository
    {
        private readonly AisamContext _context;

        public TeamBrandRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<TeamBrand> AddAsync(TeamBrand teamBrand)
        {
            _context.TeamBrands.Add(teamBrand);
            await _context.SaveChangesAsync();
            return teamBrand;
        }

        public async Task<IEnumerable<TeamBrand>> GetByTeamIdAsync(Guid teamId)
        {
            return await _context.TeamBrands
                .Include(tb => tb.Brand)
                .Include(tb => tb.Team)
                .Where(tb => tb.TeamId == teamId && tb.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<TeamBrand>> GetByBrandIdAsync(Guid brandId)
        {
            return await _context.TeamBrands
                .Include(tb => tb.Brand)
                .Include(tb => tb.Team)
                .Where(tb => tb.BrandId == brandId && tb.IsActive)
                .ToListAsync();
        }

        public async Task<TeamBrand?> GetByTeamAndBrandAsync(Guid teamId, Guid brandId)
        {
            return await _context.TeamBrands
                .FirstOrDefaultAsync(tb => tb.TeamId == teamId && tb.BrandId == brandId && tb.IsActive);
        }

        public async Task<bool> DeleteAsync(Guid teamId, Guid brandId)
        {
            var teamBrand = await _context.TeamBrands
                .FirstOrDefaultAsync(tb => tb.TeamId == teamId && tb.BrandId == brandId && tb.IsActive);

            if (teamBrand == null)
                return false;

            teamBrand.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CreateTeamBrandAssociationsAsync(Guid teamId, IEnumerable<Guid> brandIds, Guid userId)
        {
            var validBrandIds = new List<Guid>();

            // Validate that brands belong to the user
            foreach (var brandId in brandIds)
            {
                var brand = await _context.Brands
                    .FirstOrDefaultAsync(b => b.Id == brandId && b.UserId == userId && !b.IsDeleted);

                if (brand != null)
                {
                    validBrandIds.Add(brandId);
                }
            }

            // Create TeamBrand associations for valid brands
            foreach (var brandId in validBrandIds)
            {
                var teamBrand = new TeamBrand
                {
                    TeamId = teamId,
                    BrandId = brandId
                };
                await _context.TeamBrands.AddAsync(teamBrand);
            }

            // Save changes and return count of created associations
            await _context.SaveChangesAsync();
            return validBrandIds.Count;
        }

        public async Task<int> SoftDeleteByTeamIdAsync(Guid teamId)
        {
            var teamBrands = await _context.TeamBrands
                .Where(tb => tb.TeamId == teamId && tb.IsActive)
                .ToListAsync();

            if (!teamBrands.Any())
                return 0;

            // Soft delete all team brand associations by setting IsActive = false
            foreach (var teamBrand in teamBrands)
            {
                teamBrand.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return teamBrands.Count;
        }

        public async Task UpdateAsync(TeamBrand teamBrand)
        {
            _context.TeamBrands.Update(teamBrand);
            await _context.SaveChangesAsync();
        }
    }
}
