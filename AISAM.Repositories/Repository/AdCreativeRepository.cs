using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class AdCreativeRepository : IAdCreativeRepository
    {
        private readonly AisamContext _context;

        public AdCreativeRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<AdCreative?> GetByIdAsync(Guid id)
        {
            return await _context.AdCreatives
                .FirstOrDefaultAsync(ac => ac.Id == id && !ac.IsDeleted);
        }

        public async Task<AdCreative?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.AdCreatives
                .Include(ac => ac.Content)
                    .ThenInclude(c => c.Brand)
                .Include(ac => ac.Ads.Where(a => !a.IsDeleted))
                    .ThenInclude(a => a.AdSet)
                        .ThenInclude(ads => ads.Campaign)
                            .ThenInclude(c => c.Brand)
                .FirstOrDefaultAsync(ac => ac.Id == id && !ac.IsDeleted);
        }

        public async Task<AdCreative?> GetByContentIdAsync(Guid contentId)
        {
            return await _context.AdCreatives
                .Include(ac => ac.Content)
                .FirstOrDefaultAsync(ac => ac.ContentId == contentId && !ac.IsDeleted);
        }

        public async Task<(IEnumerable<AdCreative> Data, int TotalCount)> GetByAdSetIdPagedAsync(Guid adSetId, int page, int pageSize, string? search = null, string? type = null, string? sortBy = null, string? sortOrder = null)
        {
            var query = _context.AdCreatives
                .Include(ac => ac.Content)
                .Include(ac => ac.Ads)
                .Where(ac => !ac.IsDeleted && ac.Ads.Any(a => a.AdSetId == adSetId))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(ac => (ac.Content != null && ac.Content.Title.Contains(search)) || (ac.CallToAction != null && ac.CallToAction.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                // Filter by related content's AdType when available
                var t = type.ToUpper();
                query = query.Where(ac => ac.Content != null && ac.Content.AdType.ToString().ToUpper() == t);
            }

            // Sorting
            var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortBy?.ToLower())
            {
                case "createdat":
                    query = desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt);
                    break;
                default:
                    query = query.OrderByDescending(x => x.CreatedAt);
                    break;
            }

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (data, total);
        }

        public async Task<AdCreative> CreateAsync(AdCreative adCreative)
        {
            _context.AdCreatives.Add(adCreative);
            await _context.SaveChangesAsync();
            return adCreative;
        }

        public async Task UpdateAsync(AdCreative adCreative)
        {
            _context.AdCreatives.Update(adCreative);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var adCreative = await _context.AdCreatives.FindAsync(id);
            if (adCreative == null) return false;

            adCreative.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
