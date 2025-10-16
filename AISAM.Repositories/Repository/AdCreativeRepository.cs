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
                .FirstOrDefaultAsync(ac => ac.Id == id && !ac.IsDeleted);
        }

        public async Task<AdCreative?> GetByContentIdAsync(Guid contentId)
        {
            return await _context.AdCreatives
                .Include(ac => ac.Content)
                .FirstOrDefaultAsync(ac => ac.ContentId == contentId && !ac.IsDeleted);
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
