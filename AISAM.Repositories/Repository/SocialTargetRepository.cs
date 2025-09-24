using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class SocialTargetRepository : ISocialTargetRepository
    {
        private readonly AISAMContext _context;

        public SocialTargetRepository(AISAMContext context)
        {
            _context = context;
        }

        public async Task<SocialTarget?> GetByIdAsync(Guid id)
        {
            return await _context.SocialTargets
                .Include(st => st.SocialAccount)
                .ThenInclude(sa => sa.User)
                .FirstOrDefaultAsync(st => st.Id == id);
        }

        public async Task<SocialTarget?> GetByProviderTargetIdAsync(Guid socialAccountId, string providerTargetId)
        {
            return await _context.SocialTargets
                .Include(st => st.SocialAccount)
                .FirstOrDefaultAsync(st => st.SocialAccountId == socialAccountId && st.ProviderTargetId == providerTargetId);
        }

        public async Task<SocialTarget?> GetByProviderTargetIdAsync(string providerTargetId)
        {
            return await _context.SocialTargets
                .Include(st => st.SocialAccount)
                .FirstOrDefaultAsync(st => st.ProviderTargetId == providerTargetId);
        }

        public async Task<IEnumerable<SocialTarget>> GetBySocialAccountIdAsync(Guid socialAccountId)
        {
            return await _context.SocialTargets
                .Where(st => st.SocialAccountId == socialAccountId)
                .ToListAsync();
        }

        public async Task<IEnumerable<SocialTarget>> GetByUserIdAsync(Guid userId)
        {
            return await _context.SocialTargets
                .Include(st => st.SocialAccount)
                .Where(st => st.SocialAccount.UserId == userId)
                .ToListAsync();
        }

        public async Task<SocialTarget> CreateAsync(SocialTarget target)
        {
            target.CreatedAt = DateTime.UtcNow;
            target.UpdatedAt = DateTime.UtcNow;
            
            _context.SocialTargets.Add(target);
            await _context.SaveChangesAsync();
            return target;
        }

        public async Task UpdateAsync(SocialTarget target)
        {
            target.UpdatedAt = DateTime.UtcNow;
            _context.SocialTargets.Update(target);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var target = await _context.SocialTargets.FindAsync(id);
            if (target != null)
            {
                _context.SocialTargets.Remove(target);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteBySocialAccountIdAsync(Guid socialAccountId)
        {
            var targets = await _context.SocialTargets
                .Where(st => st.SocialAccountId == socialAccountId)
                .ToListAsync();
            
            _context.SocialTargets.RemoveRange(targets);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(Guid socialAccountId, string providerTargetId)
        {
            return await _context.SocialTargets
                .AnyAsync(st => st.SocialAccountId == socialAccountId && st.ProviderTargetId == providerTargetId);
        }
    }
}