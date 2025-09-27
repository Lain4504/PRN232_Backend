using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class SocialAccountRepository : ISocialAccountRepository
    {
        private readonly AisamContext _context;

        public SocialAccountRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<SocialAccount?> GetByIdAsync(Guid id)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialTargets)
                .FirstOrDefaultAsync(sa => sa.Id == id);
        }

        public async Task<SocialAccount?> GetByIdWithTargetsAsync(Guid id)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialTargets)
                .FirstOrDefaultAsync(sa => sa.Id == id);
        }

        public async Task<SocialAccount?> GetByProviderAndUserIdAsync(string provider, string providerUserId)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialTargets)
                .FirstOrDefaultAsync(sa => sa.Provider == provider && sa.ProviderUserId == providerUserId);
        }

        public async Task<SocialAccount?> GetByUserIdAndProviderAsync(Guid userId, string provider)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialTargets)
                .FirstOrDefaultAsync(sa => sa.UserId == userId && sa.Provider == provider);
        }

        public async Task<IEnumerable<SocialAccount>> GetByUserIdAsync(Guid userId)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.SocialTargets)
                .Where(sa => sa.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<SocialAccount>> GetByProviderAsync(string provider)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialTargets)
                .Where(sa => sa.Provider == provider)
                .ToListAsync();
        }

        public async Task<SocialAccount> CreateAsync(SocialAccount account)
        {
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            
            _context.SocialAccounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task UpdateAsync(SocialAccount account)
        {
            account.UpdatedAt = DateTime.UtcNow;
            _context.SocialAccounts.Update(account);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var account = await _context.SocialAccounts
                .Include(sa => sa.SocialTargets)
                .FirstOrDefaultAsync(sa => sa.Id == id);
                
            if (account != null)
            {
                // First, delete all posts associated with the social targets
                var targetIds = account.SocialTargets.Select(st => st.Id).ToList();
                if (targetIds.Any())
                {
                    var postsToDelete = await _context.Posts
                        .Where(sp => sp.SocialTargetId.HasValue && targetIds.Contains(sp.SocialTargetId.Value))
                        .ToListAsync();
                    
                    if (postsToDelete.Any())
                    {
                        _context.Posts.RemoveRange(postsToDelete);
                    }
                }
                
                // Then delete the social targets
                if (account.SocialTargets.Any())
                {
                    _context.SocialTargets.RemoveRange(account.SocialTargets);
                }
                
                // Finally delete the social account
                _context.SocialAccounts.Remove(account);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(string provider, string providerUserId)
        {
            return await _context.SocialAccounts
                .AnyAsync(sa => sa.Provider == provider && sa.ProviderUserId == providerUserId);
        }
    }
}