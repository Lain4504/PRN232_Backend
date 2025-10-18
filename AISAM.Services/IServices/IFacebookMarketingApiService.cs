using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IFacebookMarketingApiService
    {
        Task<string> CreateCampaignAsync(string adAccountId, string name, string objective, decimal budget, string accessToken);
        Task<string> CreateAdSetAsync(string adAccountId, string campaignId, string name, string targeting, decimal dailyBudget, string accessToken);
        Task<string> CreateAdCreativeAsync(string adAccountId, string pageId, string message, string? imageUrl, string? videoUrl, string? callToAction, string accessToken);
        Task<string> CreateAdAsync(string adAccountId, string adSetId, string creativeId, string status, string accessToken);
        Task<bool> UpdateAdStatusAsync(string adId, string status, string accessToken);
        Task<bool> DeleteAdAsync(string adId, string accessToken);
        Task<bool> DeleteCampaignAsync(string adAccountId, string campaignId, string accessToken, string deleteStrategy = "DELETE_ANY", DateTime? beforeDate = null, int? objectCount = null);
        Task<AdPerformanceResponse?> GetAdInsightsAsync(string adId, string accessToken);
        Task<bool> CheckTokenExpiryAsync(string accessToken);
        Task<string> BuildTargetingJson(int? minAge, int? maxAge, string? gender, string? country, string[]? interests);
    }
}
