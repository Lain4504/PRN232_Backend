using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IAdCampaignService
    {
        Task<AdCampaignResponse> CreateCampaignAsync(Guid userId, CreateAdCampaignRequest request);
        Task<PagedResult<AdCampaignResponse>> GetCampaignsAsync(Guid userId, Guid? brandId, Guid? teamId = null, int page = 1, int pageSize = 20);
        Task<AdCampaignResponse?> GetCampaignByIdAsync(Guid userId, Guid campaignId);
        Task<bool> DeleteCampaignAsync(Guid userId, Guid campaignId);
    }
}
