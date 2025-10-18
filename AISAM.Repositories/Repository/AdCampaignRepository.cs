using AISAM.Common.Dtos;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class AdCampaignRepository : IAdCampaignRepository
    {
        private readonly AisamContext _context;

        public AdCampaignRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<AdCampaign?> GetByIdAsync(Guid id)
        {
            return await _context.AdCampaigns
                .FirstOrDefaultAsync(ac => ac.Id == id && !ac.IsDeleted);
        }

        public async Task<AdCampaign?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.AdCampaigns
                .Include(ac => ac.Brand)
                .Include(ac => ac.User)
                .Include(ac => ac.AdSets.Where(ads => !ads.IsDeleted))
                    .ThenInclude(ads => ads.Ads.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(ac => ac.Id == id && !ac.IsDeleted);
        }

        public async Task<PagedResult<AdCampaign>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var query = _context.AdCampaigns
                .Include(ac => ac.Brand)
                .Include(ac => ac.AdSets.Where(ads => !ads.IsDeleted))
                .Where(ac => ac.UserId == userId && !ac.IsDeleted)
                .OrderByDescending(ac => ac.CreatedAt);

            var totalCount = await query.CountAsync();
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AdCampaign>
            {
                Data = data,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<AdCampaign>> GetByBrandIdAsync(Guid brandId, int page = 1, int pageSize = 20)
        {
            var query = _context.AdCampaigns
                .Include(ac => ac.Brand)
                .Include(ac => ac.User)
                .Include(ac => ac.AdSets.Where(ads => !ads.IsDeleted))
                .Where(ac => ac.BrandId == brandId && !ac.IsDeleted)
                .OrderByDescending(ac => ac.CreatedAt);

            var totalCount = await query.CountAsync();
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AdCampaign>
            {
                Data = data,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AdCampaign> CreateAsync(AdCampaign adCampaign)
        {
            _context.AdCampaigns.Add(adCampaign);
            await _context.SaveChangesAsync();
            return adCampaign;
        }

        public async Task UpdateAsync(AdCampaign adCampaign)
        {
            adCampaign.UpdatedAt = DateTime.UtcNow;
            _context.AdCampaigns.Update(adCampaign);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var adCampaign = await _context.AdCampaigns.FindAsync(id);
            if (adCampaign == null) return false;

            adCampaign.IsDeleted = true;
            adCampaign.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountActiveByUserIdAsync(Guid userId)
        {
            return await _context.AdCampaigns
                .CountAsync(ac => ac.UserId == userId && !ac.IsDeleted && ac.IsActive);
        }

        public async Task<int> CountActiveByBrandIdAsync(Guid brandId)
        {
            return await _context.AdCampaigns
                .CountAsync(ac => ac.BrandId == brandId && !ac.IsDeleted && ac.IsActive);
        }
    }
}
