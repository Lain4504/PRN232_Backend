using AISAM.Data.Model;
using AISAM.Data.Enumeration;

namespace AISAM.Repositories.IRepositories
{
    public interface ISocialIntegrationsRepository
    {
        Task<SocialIntegration?> GetActiveForBrandAsync(Guid brandId, SocialPlatformEnum platform, CancellationToken ct);
    }
}


