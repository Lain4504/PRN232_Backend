using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repositories.Repository
{
    public class SocialAccountRepository : ISocialAccountRepository
    {
        private readonly AISAMContext _context;

        public SocialAccountRepository(AISAMContext context)
        {
            _context = context;
        }

        public async Task<SocialAccount?> GetByIdAsync(int id)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialTargets)
                .FirstOrDefaultAsync(sa => sa.Id == id);
        }

        public async Task<SocialAccount?> GetByIdWithTargetsAsync(int id)
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

        public async Task<SocialAccount?> GetByUserIdAndProviderAsync(int userId, string provider)
        {
            return await _context.SocialAccounts
                .Include(sa => sa.User)
                .Include(sa => sa.SocialTargets)
                .FirstOrDefaultAsync(sa => sa.UserId == userId && sa.Provider == provider);
        }

        public async Task<IEnumerable<SocialAccount>> GetByUserIdAsync(int userId)
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

        public async Task DeleteAsync(int id)
        {
            var account = await _context.SocialAccounts.FindAsync(id);
            if (account != null)
            {
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