using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class AdCreativesRepository : IAdCreativesRepository
    {
        private readonly AisamContext _context;

        public AdCreativesRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<AdCreative> AddAsync(AdCreative entity, CancellationToken ct)
        {
            _context.AdCreatives.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public Task<AdCreative?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return _context.AdCreatives.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        }

        public Task<List<AdCreative>> ListByAccountAsync(string adAccountId, CancellationToken ct)
        {
            return _context.AdCreatives.AsNoTracking()
                .Where(x => x.AdAccountId == adAccountId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(AdCreative entity, CancellationToken ct)
        {
            _context.AdCreatives.Update(entity);
            await _context.SaveChangesAsync(ct);
        }

        public async Task SoftDeleteAsync(AdCreative entity, CancellationToken ct)
        {
            entity.IsDeleted = true;
            _context.AdCreatives.Update(entity);
            await _context.SaveChangesAsync(ct);
        }
    }
}


