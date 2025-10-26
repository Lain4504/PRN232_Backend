using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class AdSetRepository : IAdSetRepository
    {
        private readonly AisamContext _context;

        public AdSetRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<AdSet?> GetByIdAsync(Guid id)
        {
            return await _context.AdSets
                .FirstOrDefaultAsync(ads => ads.Id == id && !ads.IsDeleted);
        }

        public async Task<AdSet?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.AdSets
                .Include(ads => ads.Campaign)
                    .ThenInclude(c => c.Brand)
                .Include(ads => ads.Ads.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(ads => ads.Id == id && !ads.IsDeleted);
        }

        public async Task<List<AdSet>> GetByCampaignIdAsync(Guid campaignId)
        {
            return await _context.AdSets
                .Include(ads => ads.Ads.Where(a => !a.IsDeleted))
                .Where(ads => ads.CampaignId == campaignId && !ads.IsDeleted)
                .OrderByDescending(ads => ads.CreatedAt)
                .ToListAsync();
        }

        public async Task<AdSet> CreateAsync(AdSet adSet)
        {
            _context.AdSets.Add(adSet);
            await _context.SaveChangesAsync();
            return adSet;
        }

        public async Task UpdateAsync(AdSet adSet)
        {
            _context.AdSets.Update(adSet);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateStatusAsync(Guid id, string status)
        {
            var adSet = await _context.AdSets.FindAsync(id);
            if (adSet == null) return false;

            adSet.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var adSet = await _context.AdSets.FindAsync(id);
            if (adSet == null) return false;

            adSet.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
