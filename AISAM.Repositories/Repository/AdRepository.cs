using AISAM.Common.Dtos;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class AdRepository : IAdRepository
    {
        private readonly AisamContext _context;

        public AdRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Ad?> GetByIdAsync(Guid id)
        {
            return await _context.Ads
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<Ad?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.Ads
                .Include(a => a.AdSet)
                    .ThenInclude(ads => ads.Campaign)
                        .ThenInclude(c => c.Brand)
                .Include(a => a.Creative)
                    .ThenInclude(c => c.Content)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<List<Ad>> GetByAdSetIdAsync(Guid adSetId)
        {
            return await _context.Ads
                .Include(a => a.Creative)
                .Where(a => a.AdSetId == adSetId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<Ad>> GetByCampaignIdAsync(Guid campaignId, int page = 1, int pageSize = 20)
        {
            var query = _context.Ads
                .Include(a => a.AdSet)
                .Include(a => a.Creative)
                    .ThenInclude(c => c.Content)
                .Where(a => a.AdSet.CampaignId == campaignId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync();
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Ad>
            {
                Data = data,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<Ad>> GetByBrandIdAsync(Guid brandId, int page = 1, int pageSize = 20)
        {
            var query = _context.Ads
                .Include(a => a.AdSet)
                    .ThenInclude(ads => ads.Campaign)
                .Include(a => a.Creative)
                    .ThenInclude(c => c.Content)
                .Where(a => a.AdSet.Campaign.BrandId == brandId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync();
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Ad>
            {
                Data = data,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<Ad>> GetActiveAdsAsync()
        {
            return await _context.Ads
                .Include(a => a.AdSet)
                    .ThenInclude(ads => ads.Campaign)
                        .ThenInclude(c => c.Brand)
                            .ThenInclude(b => b.User)
                .Where(a => !a.IsDeleted && (a.Status == "ACTIVE" || a.Status == "PAUSED"))
                .ToListAsync();
        }

        public async Task<Ad> CreateAsync(Ad ad)
        {
            _context.Ads.Add(ad);
            await _context.SaveChangesAsync();
            return ad;
        }

        public async Task UpdateAsync(Ad ad)
        {
            _context.Ads.Update(ad);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateStatusAsync(Guid id, string status)
        {
            var ad = await _context.Ads.FindAsync(id);
            if (ad == null) return false;

            ad.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var ad = await _context.Ads.FindAsync(id);
            if (ad == null) return false;

            ad.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
