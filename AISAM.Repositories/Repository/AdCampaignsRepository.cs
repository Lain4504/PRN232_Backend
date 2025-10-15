using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class AdCampaignsRepository : IAdCampaignsRepository
    {
        private readonly AisamContext _context;

        public AdCampaignsRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<AdCampaign> AddAsync(AdCampaign entity, CancellationToken ct)
        {
            _context.AdCampaigns.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public Task<AdCampaign?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return _context.AdCampaigns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        }

        public Task<List<AdCampaign>> ListAsync(Guid? userId, Guid? brandId, CancellationToken ct)
        {
            var q = _context.AdCampaigns.AsNoTracking().Where(x => !x.IsDeleted);
            if (userId.HasValue) q = q.Where(x => x.UserId == userId);
            if (brandId.HasValue) q = q.Where(x => x.BrandId == brandId);
            return q.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        }

        public async Task UpdateAsync(AdCampaign entity, CancellationToken ct)
        {
            _context.AdCampaigns.Update(entity);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(AdCampaign entity, CancellationToken ct)
        {
            entity.IsDeleted = true;
            _context.AdCampaigns.Update(entity);
            await _context.SaveChangesAsync(ct);
        }
    }
}


