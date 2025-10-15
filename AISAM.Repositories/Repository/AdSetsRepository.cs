using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class AdSetsRepository : IAdSetsRepository
    {
        private readonly AisamContext _context;

        public AdSetsRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<AdSet> AddAsync(AdSet entity, CancellationToken ct)
        {
            _context.AdSets.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public Task<AdSet?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return _context.AdSets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        }

        public Task<List<AdSet>> ListByCampaignAsync(Guid campaignId, CancellationToken ct)
        {
            return _context.AdSets.AsNoTracking()
                .Where(x => x.CampaignId == campaignId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(AdSet entity, CancellationToken ct)
        {
            _context.AdSets.Update(entity);
            await _context.SaveChangesAsync(ct);
        }

        public async Task SoftDeleteAsync(AdSet entity, CancellationToken ct)
        {
            entity.IsDeleted = true;
            _context.AdSets.Update(entity);
            await _context.SaveChangesAsync(ct);
        }
    }
}


