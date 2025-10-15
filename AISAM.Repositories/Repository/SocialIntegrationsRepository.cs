using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class SocialIntegrationsRepository : ISocialIntegrationsRepository
    {
        private readonly AisamContext _context;

        public SocialIntegrationsRepository(AisamContext context)
        {
            _context = context;
        }

        public Task<SocialIntegration?> GetActiveForBrandAsync(Guid brandId, SocialPlatformEnum platform, CancellationToken ct)
        {
            return _context.SocialIntegrations.AsNoTracking()
                .Where(x => x.BrandId == brandId && x.Platform == platform && x.IsActive && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }
    }
}


