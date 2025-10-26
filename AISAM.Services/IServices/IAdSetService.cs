using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IAdSetService
    {
        Task<AdSetResponse> CreateAdSetAsync(Guid userId, CreateAdSetRequest request);
        Task<List<AdSetResponse>> GetAdSetsByCampaignAsync(Guid userId, Guid campaignId);
        Task<AdSetResponse?> GetAdSetByIdAsync(Guid userId, Guid adSetId);
        Task<bool> UpdateAdSetStatusAsync(Guid userId, UpdateAdSetStatusRequest request);
        Task<bool> DeleteAdSetAsync(Guid userId, Guid adSetId);
    }
}
