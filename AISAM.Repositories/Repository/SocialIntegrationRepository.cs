using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class SocialIntegrationRepository : ISocialIntegrationRepository
    {
        private readonly AisamContext _context;

        public SocialIntegrationRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<SocialIntegration?> GetByIdAsync(Guid id)
        {
            return await _context.SocialIntegrations
                .Include(si => si.SocialAccount)
                .Include(si => si.Profile)
                .Include(si => si.Brand)
                .Include(si => si.Posts)
                .FirstOrDefaultAsync(si => si.Id == id);
        }

        public async Task<SocialIntegration?> GetByExternalIdAsync(string externalId)
        {
            return await _context.SocialIntegrations
                .Include(si => si.SocialAccount)
                .Include(si => si.Profile)
                .Include(si => si.Brand)
                .Include(si => si.Posts)
                .FirstOrDefaultAsync(si => si.ExternalId == externalId);
        }

        public async Task<IEnumerable<SocialIntegration>> GetBySocialAccountIdAsync(Guid socialAccountId)
        {
            return await _context.SocialIntegrations
                .Include(si => si.SocialAccount)
                .Include(si => si.Profile)
                .Include(si => si.Brand)
                .Include(si => si.Posts)
                .Where(si => si.SocialAccountId == socialAccountId)
                .ToListAsync();
        }

        public async Task<IEnumerable<SocialIntegration>> GetByUserIdAsync(Guid userId)
        {
            return await _context.SocialIntegrations
                .Include(si => si.SocialAccount)
                .Include(si => si.Profile)
                .Include(si => si.Brand)
                .Include(si => si.Posts)
                .Where(si => si.Profile.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<SocialIntegration>> GetByProfileIdAsync(Guid profileId)
        {
            return await _context.SocialIntegrations
                .Include(si => si.SocialAccount)
                .Include(si => si.Profile)
                .Include(si => si.Brand)
                .Include(si => si.Posts)
                .Where(si => si.ProfileId == profileId)
                .ToListAsync();
        }

        public async Task<SocialIntegration?> GetByBrandIdAsync(Guid brandId)
        {
            return await _context.SocialIntegrations
                .Include(si => si.SocialAccount)
                .Include(si => si.Profile)
                .Include(si => si.Brand)
                .FirstOrDefaultAsync(si => si.BrandId == brandId && si.IsActive && !si.IsDeleted);
        }

        public async Task<SocialIntegration?> GetByAdAccountIdAsync(string adAccountId)
        {
            return await _context.SocialIntegrations
                .Include(si => si.SocialAccount)
                .Include(si => si.Profile)
                .Include(si => si.Brand)
                .FirstOrDefaultAsync(si => si.AdAccountId == adAccountId && si.IsActive && !si.IsDeleted);
        }

        public async Task<SocialIntegration> CreateAsync(SocialIntegration integration)
        {
            integration.CreatedAt = DateTime.UtcNow;
            integration.UpdatedAt = DateTime.UtcNow;
            
            _context.SocialIntegrations.Add(integration);
            await _context.SaveChangesAsync();
            return integration;
        }

        public async Task UpdateAsync(SocialIntegration integration)
        {
            integration.UpdatedAt = DateTime.UtcNow;
            _context.SocialIntegrations.Update(integration);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var integration = await _context.SocialIntegrations
                .Include(si => si.Posts)
                .FirstOrDefaultAsync(si => si.Id == id);
                
            if (integration != null)
            {
                // First, delete all posts associated with this integration
                if (integration.Posts.Any())
                {
                    _context.Posts.RemoveRange(integration.Posts);
                }
                
                // Then delete the integration
                _context.SocialIntegrations.Remove(integration);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteBySocialAccountIdAsync(Guid socialAccountId)
        {
            var integrations = await _context.SocialIntegrations
                .Include(si => si.Posts)
                .Where(si => si.SocialAccountId == socialAccountId)
                .ToListAsync();
                
            if (integrations.Any())
            {
                // First, delete all posts associated with these integrations
                var allPosts = integrations.SelectMany(si => si.Posts).ToList();
                if (allPosts.Any())
                {
                    _context.Posts.RemoveRange(allPosts);
                }
                
                // Then delete the integrations
                _context.SocialIntegrations.RemoveRange(integrations);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid socialAccountId, string externalId)
        {
            return await _context.SocialIntegrations
                .AnyAsync(si => si.SocialAccountId == socialAccountId && si.ExternalId == externalId);
        }
    }
}
