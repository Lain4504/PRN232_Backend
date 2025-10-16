using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IAdService
    {
        Task<AdResponse> CreateAdAsync(Guid userId, CreateAdRequest request);
        Task<PagedResult<AdResponse>> GetAdsAsync(Guid userId, Guid? campaignId, Guid? brandId, int page = 1, int pageSize = 20);
        Task<AdResponse?> GetAdByIdAsync(Guid userId, Guid adId);
        Task<bool> UpdateAdStatusAsync(Guid userId, UpdateAdStatusRequest request);
        Task<bool> DeleteAdAsync(Guid userId, Guid adId);
        Task<AdPerformanceResponse?> PullReportsAsync(Guid userId, Guid adId);
        Task PullAllActiveAdsReportsAsync();
    }
}
