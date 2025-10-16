using System.Text;
using System.Text.Json;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class FacebookMarketingApiService : IFacebookMarketingApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FacebookMarketingApiService> _logger;
        private const string BaseUrl = "https://graph.facebook.com/v20.0";

        public FacebookMarketingApiService(HttpClient httpClient, ILogger<FacebookMarketingApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> CreateCampaignAsync(string adAccountId, string name, string objective, decimal budget, string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/act_{adAccountId}/campaigns";
                
                var payload = new
                {
                    name = name,
                    objective = objective,
                    status = "PAUSED",
                    daily_budget = (int)(budget * 100), // Convert to cents
                    access_token = accessToken
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create campaign: {Error}", errorContent);
                    throw new Exception($"Facebook API Error: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                if (jsonResponse.RootElement.TryGetProperty("id", out var idProperty))
                {
                    return idProperty.GetString() ?? throw new Exception("Campaign ID not found in response");
                }

                throw new Exception("Invalid response from Facebook API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campaign for ad account {AdAccountId}", adAccountId);
                throw;
            }
        }

        public async Task<string> CreateAdSetAsync(string adAccountId, string campaignId, string name, string targeting, decimal dailyBudget, string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/act_{adAccountId}/adsets";
                
                var targetingObj = JsonSerializer.Deserialize<object>(targeting);
                
                var payload = new
                {
                    name = name,
                    campaign_id = campaignId,
                    targeting = targetingObj,
                    daily_budget = (int)(dailyBudget * 100), // Convert to cents
                    status = "PAUSED",
                    access_token = accessToken
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create ad set: {Error}", errorContent);
                    throw new Exception($"Facebook API Error: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                if (jsonResponse.RootElement.TryGetProperty("id", out var idProperty))
                {
                    return idProperty.GetString() ?? throw new Exception("Ad Set ID not found in response");
                }

                throw new Exception("Invalid response from Facebook API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad set for campaign {CampaignId}", campaignId);
                throw;
            }
        }

        public async Task<string> CreateAdCreativeAsync(string adAccountId, string pageId, string message, string? imageUrl, string? videoUrl, string? callToAction, string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/act_{adAccountId}/adcreatives";
                
                var objectStorySpec = new
                {
                    page_id = pageId,
                    link_data = new
                    {
                        message = message,
                        picture = imageUrl,
                        video_data = !string.IsNullOrEmpty(videoUrl) ? new { video_id = videoUrl } : null,
                        call_to_action = !string.IsNullOrEmpty(callToAction) ? new { type = callToAction } : null
                    }
                };

                var payload = new
                {
                    object_story_spec = objectStorySpec,
                    access_token = accessToken
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create ad creative: {Error}", errorContent);
                    throw new Exception($"Facebook API Error: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                if (jsonResponse.RootElement.TryGetProperty("id", out var idProperty))
                {
                    return idProperty.GetString() ?? throw new Exception("Creative ID not found in response");
                }

                throw new Exception("Invalid response from Facebook API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad creative for page {PageId}", pageId);
                throw;
            }
        }

        public async Task<string> CreateAdAsync(string adAccountId, string adSetId, string creativeId, string status, string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/act_{adAccountId}/ads";
                
                var payload = new
                {
                    name = $"Ad for {adSetId}",
                    adset_id = adSetId,
                    creative = new { creative_id = creativeId },
                    status = status,
                    access_token = accessToken
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create ad: {Error}", errorContent);
                    throw new Exception($"Facebook API Error: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                if (jsonResponse.RootElement.TryGetProperty("id", out var idProperty))
                {
                    return idProperty.GetString() ?? throw new Exception("Ad ID not found in response");
                }

                throw new Exception("Invalid response from Facebook API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad for ad set {AdSetId}", adSetId);
                throw;
            }
        }

        public async Task<bool> UpdateAdStatusAsync(string adId, string status, string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/{adId}";
                
                var payload = new
                {
                    status = status,
                    access_token = accessToken
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update ad status: {Error}", errorContent);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ad status for ad {AdId}", adId);
                return false;
            }
        }

        public async Task<bool> DeleteAdAsync(string adId, string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/{adId}?access_token={accessToken}";
                var response = await _httpClient.DeleteAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete ad: {Error}", errorContent);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ad {AdId}", adId);
                return false;
            }
        }

        public async Task<AdPerformanceResponse?> GetAdInsightsAsync(string adId, string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/{adId}/insights?fields=impressions,clicks,ctr,spend,actions&access_token={accessToken}";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get ad insights: {Error}", errorContent);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                var data = jsonResponse.RootElement.GetProperty("data");
                if (data.GetArrayLength() == 0)
                {
                    return null;
                }

                var insight = data[0];
                var actions = insight.TryGetProperty("actions", out var actionsProp) ? actionsProp : (JsonElement?)null;

                long engagement = 0;
                if (actions.HasValue && actions.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var action in actions.Value.EnumerateArray())
                    {
                        if (action.TryGetProperty("action_type", out var actionType) && 
                            (actionType.GetString() == "like" || actionType.GetString() == "comment" || actionType.GetString() == "share"))
                        {
                            engagement += action.TryGetProperty("value", out var value) ? value.GetInt64() : 0;
                        }
                    }
                }

                return new AdPerformanceResponse
                {
                    AdId = Guid.Parse(adId),
                    Impressions = insight.TryGetProperty("impressions", out var impressions) ? impressions.GetInt64() : 0,
                    Clicks = insight.TryGetProperty("clicks", out var clicks) ? clicks.GetInt64() : 0,
                    Ctr = insight.TryGetProperty("ctr", out var ctr) ? ctr.GetDecimal() : 0,
                    Spend = insight.TryGetProperty("spend", out var spend) ? spend.GetDecimal() : 0,
                    Engagement = engagement,
                    EstimatedRevenue = insight.TryGetProperty("spend", out var spend2) ? spend2.GetDecimal() : 0, // Using spend as revenue proxy
                    ReportDate = DateTime.UtcNow.Date,
                    RawData = responseContent
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting insights for ad {AdId}", adId);
                return null;
            }
        }

        public async Task<bool> CheckTokenExpiryAsync(string accessToken)
        {
            try
            {
                var url = $"{BaseUrl}/me?access_token={accessToken}";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token expiry");
                return false;
            }
        }

        public Task<string> BuildTargetingJson(int? minAge, int? maxAge, string? gender, string? country, string[]? interests)
        {
            var targeting = new Dictionary<string, object>();

            if (minAge.HasValue || maxAge.HasValue)
            {
                targeting["age_min"] = minAge ?? 18;
                targeting["age_max"] = maxAge ?? 65;
            }

            if (!string.IsNullOrEmpty(gender))
            {
                targeting["genders"] = new[] { gender.ToUpper() };
            }

            if (!string.IsNullOrEmpty(country))
            {
                targeting["geo_locations"] = new
                {
                    countries = new[] { country.ToUpper() }
                };
            }

            if (interests != null && interests.Length > 0)
            {
                targeting["interests"] = interests.Select(i => new { id = i, name = i }).ToArray();
            }

            return Task.FromResult(JsonSerializer.Serialize(targeting));
        }
    }
}
