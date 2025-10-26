using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IFacebookMarketingApiService
    {
        Task<string> CreateCampaignAsync(string adAccountId, string name, string objective, decimal budget, string accessToken);
        Task<string> CreateAdSetAsync(string adAccountId, string campaignId, string name, string targeting, decimal dailyBudget, string accessToken, DateTime? startTime = null, DateTime? endTime = null, string? optimizationGoal = null, string? billingEvent = null);
        Task<string> CreateAdCreativeAsync(string adAccountId, string pageId, string message, string? imageUrl, string? videoUrl, string? callToAction, string accessToken, string? linkUrl = null, string? adName = null);
        Task<string> CreateAdAsync(string adAccountId, string facebookAdSetId, string facebookCreativeId, string status, string accessToken, string? adName = null);
        Task<bool> UpdateAdStatusAsync(string adId, string status, string accessToken);
        Task<bool> UpdateAdSetStatusAsync(string adSetId, string status, string accessToken);
        Task<bool> DeleteAdAsync(string adId, string accessToken);
        Task<bool> DeleteCampaignAsync(string adAccountId, string campaignId, string accessToken, string deleteStrategy = "DELETE_ANY", DateTime? beforeDate = null, int? objectCount = null);
        Task<bool> DeleteAdSetAsync(string adSetId, string accessToken);
        Task<AdPerformanceResponse?> GetAdInsightsAsync(string adId, string accessToken);
        Task<bool> CheckTokenExpiryAsync(string accessToken);
        Task<string> BuildTargetingJson(int? minAge, int? maxAge, string? gender, string? country, string[]? interests);
        Task<FacebookPostDetails?> GetPostDetailsAsync(string postId, string accessToken);
        Task<string> CreateAdCreativeFromPostAsync(string adAccountId, string postId, string accessToken, string? adName = null);
    }
}
