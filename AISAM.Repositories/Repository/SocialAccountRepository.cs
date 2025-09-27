using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using AISAM.Data.Model;

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
                .Include(sa => sa.SocialIntegrations)
                .FirstOrDefaultAsync(sa => sa.Id == id);
        }

        public async Task<SocialAccount?> GetByIdWithIntegrationsAsync(Guid id)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialIntegrations)
                .FirstOrDefaultAsync(sa => sa.Id == id);
        }

        public async Task<SocialAccount?> GetByPlatformAndAccountIdAsync(SocialPlatformEnum platform, string accountId)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialIntegrations)
                .FirstOrDefaultAsync(sa => sa.Platform == platform && sa.AccountId == accountId);
        }

        public async Task<SocialAccount?> GetByUserIdAndPlatformAsync(Guid userId, SocialPlatformEnum platform)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialIntegrations)
                .FirstOrDefaultAsync(sa => sa.UserId == userId && sa.Platform == platform);
        }

        public async Task<IEnumerable<SocialAccount>> GetByUserIdAsync(Guid userId)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.SocialIntegrations)
                .Where(sa => sa.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<SocialAccount>> GetByPlatformAsync(SocialPlatformEnum platform)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialIntegrations)
                .Where(sa => sa.Platform == platform)
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
                .Include(sa => sa.SocialIntegrations)
                .FirstOrDefaultAsync(sa => sa.Id == id);
                
            if (account != null)
            {
                // First, delete all posts associated with the social integrations
                var integrationIds = account.SocialIntegrations.Select(si => si.Id).ToList();
                if (integrationIds.Any())
                {
                    var postsToDelete = await _context.Posts
                        .Where(p => integrationIds.Contains(p.IntegrationId))
                        .ToListAsync();
                    
                    if (postsToDelete.Any())
                    {
                        _context.Posts.RemoveRange(postsToDelete);
                    }
                }
                
                // Then delete the social integrations
                if (account.SocialIntegrations.Any())
                {
                    _context.SocialIntegrations.RemoveRange(account.SocialIntegrations);
                }
                
                // Finally delete the social account
                _context.SocialAccounts.Remove(account);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(SocialPlatformEnum platform, string accountId)
        {
            return await _context.SocialAccounts
                .AnyAsync(sa => sa.Platform == platform && sa.AccountId == accountId);
        }
    }
}