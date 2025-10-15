using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IAdService
    {
        Task<CampaignResponse> CreateCampaignAsync(Guid userId, string role, CreateCampaignRequest request, CancellationToken ct);
        Task<AdSetResponse> CreateAdSetAsync(Guid userId, string role, CreateAdSetRequest request, CancellationToken ct);
        Task<CreativeResponse> CreateCreativeAsync(Guid userId, string role, CreateCreativeRequest request, CancellationToken ct);
        Task<AdResponse> PublishAdAsync(Guid userId, string role, PublishAdRequest request, CancellationToken ct);
        Task<List<CampaignResponse>> GetCampaignsAsync(Guid? userId, Guid? brandId, string role, CancellationToken ct);
        Task<List<AdResponse>> GetAdsAsync(Guid? userId, Guid? brandId, Guid? campaignId, string role, CancellationToken ct);
        Task<CampaignResponse> UpdateCampaignAsync(Guid userId, string role, Guid id, UpdateCampaignRequest request, CancellationToken ct);
        Task<AdResponse> UpdateAdAsync(Guid userId, string role, Guid id, UpdateAdRequest request, CancellationToken ct);
        Task DeleteCampaignAsync(Guid userId, string role, Guid id, CancellationToken ct);
        Task DeleteAdAsync(Guid userId, string role, Guid id, CancellationToken ct);
        Task PullReportsAsync(Guid userId, string role, Guid adId, CancellationToken ct);
    }
}


