using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class AdsRepository : IAdsRepository
    {
        private readonly AisamContext _context;

        public AdsRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Ad> AddAsync(Ad entity, CancellationToken ct)
        {
            _context.Ads.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public Task<Ad?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return _context.Ads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        }

        public Task<List<Ad>> ListAsync(Guid? userId, Guid? brandId, Guid? campaignId, CancellationToken ct)
        {
            IQueryable<Ad> q = _context.Ads.AsNoTracking().Where(x => !x.IsDeleted);

            if (campaignId.HasValue) q = q.Where(x => x.AdSet.CampaignId == campaignId);
            if (brandId.HasValue) q = q.Where(x => x.AdSet.Campaign.BrandId == brandId);
            if (userId.HasValue) q = q.Where(x => x.AdSet.Campaign.UserId == userId);

            q = q.Include(x => x.AdSet).ThenInclude(s => s.Campaign);

            return q.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        }

        public async Task UpdateAsync(Ad entity, CancellationToken ct)
        {
            _context.Ads.Update(entity);
            await _context.SaveChangesAsync(ct);
        }

        public async Task SoftDeleteAsync(Ad entity, CancellationToken ct)
        {
            entity.IsDeleted = true;
            _context.Ads.Update(entity);
            await _context.SaveChangesAsync(ct);
        }
    }
}


