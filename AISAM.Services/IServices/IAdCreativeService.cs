using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IAdCreativeService
    {
        // Legacy method - will be deprecated
        Task<AdCreativeResponse> CreateAdCreativeAsync(Guid userId, CreateAdCreativeRequest request);
        
        // New methods for specific use cases
        Task<AdCreativeResponse> CreateAdCreativeFromContentAsync(Guid userId, CreateAdCreativeFromContentRequest request);
        Task<AdCreativeResponse> CreateAdCreativeFromFacebookPostAsync(Guid userId, CreateAdCreativeFromFacebookPostRequest request);
        
        Task<AdCreativeResponse?> GetAdCreativeByIdAsync(Guid userId, Guid creativeId);
        Task<AdCreativeResponse?> GetAdCreativeByContentAsync(Guid userId, Guid contentId);
    }
}
