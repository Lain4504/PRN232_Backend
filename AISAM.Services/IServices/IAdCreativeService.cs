using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IAdCreativeService
    {
        Task<AdCreativeResponse> CreateAdCreativeAsync(Guid userId, CreateAdCreativeRequest request);
        Task<AdCreativeResponse?> GetAdCreativeByIdAsync(Guid userId, Guid creativeId);
        Task<AdCreativeResponse?> GetAdCreativeByContentAsync(Guid userId, Guid contentId);
    }
}
