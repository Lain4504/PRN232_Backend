using System.Text;
using System.Text.Json;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Services.Helper;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISAM.Services.Service
{
    public class FacebookMarketingApiService : IFacebookMarketingApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FacebookMarketingApiService> _logger;
        private readonly FacebookSettings _facebookSettings;

        public FacebookMarketingApiService(
            HttpClient httpClient, 
            ILogger<FacebookMarketingApiService> logger,
            IOptions<FacebookSettings> facebookSettings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _facebookSettings = facebookSettings.Value;
        }

        public async Task<string> CreateCampaignAsync(string adAccountId, string name, string objective, decimal budget, string accessToken)
        {
            try
            {
                // Normalize ad account ID
                var normalizedAccountId = NormalizeAdAccountId(adAccountId);
                
                // Use sandbox config if enabled
                if (_facebookSettings.UseSandbox && _facebookSettings.Sandbox != null)
                {
                    accessToken = _facebookSettings.Sandbox.AccessToken;
                    normalizedAccountId = NormalizeAdAccountId(_facebookSettings.Sandbox.AdAccountId);
                    _logger.LogInformation("Using sandbox configuration for campaign creation - Currency: {Currency}, Timezone: {Timezone}", 
                        _facebookSettings.Sandbox.Currency, _facebookSettings.Sandbox.Timezone);
                }
                
                // Map objective to Facebook API value
                var mappedObjective = FacebookObjectiveMapper.MapToFacebookObjective(objective);
                _logger.LogInformation("Mapped objective '{Objective}' to '{MappedObjective}'", objective, mappedObjective);
                
                // Convert budget to appropriate currency unit
                // For VND: 1 VND = 1 unit (no conversion needed)
                // For USD: 1 USD = 100 cents
                var budgetInCurrencyUnit = _facebookSettings.UseSandbox ? (int)budget : (int)(budget * 100);
                
                if (_facebookSettings.UseSandbox)
                {
                    _logger.LogInformation("Using VND currency for sandbox mode - budget: {Budget} VND", budget);
                }
                else
                {
                    _logger.LogInformation("Using USD currency for production mode - budget: {Budget} USD = {BudgetInCents} cents", budget, budgetInCurrencyUnit);
                }
                
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{normalizedAccountId}/campaigns";
                
                var payload = new
                {
                    name = name,
                    objective = mappedObjective,
                    status = "PAUSED",
                    buying_type = "AUCTION",
                    daily_budget = budgetInCurrencyUnit, // VND for sandbox, cents for production
                    special_ad_categories = new string[] { }, // Empty array, not ["NONE"]
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
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/act_{adAccountId}/adsets";
                
                var targetingObj = JsonSerializer.Deserialize<object>(targeting);
                
                // Convert budget to appropriate currency unit
                // For VND: 1 VND = 1 unit (no conversion needed)
                // For USD: 1 USD = 100 cents
                var budgetInCurrencyUnit = _facebookSettings.UseSandbox ? (int)dailyBudget : (int)(dailyBudget * 100);
                
                var payload = new
                {
                    name = name,
                    campaign_id = campaignId,
                    targeting = targetingObj,
                    daily_budget = budgetInCurrencyUnit, // VND for sandbox, cents for production
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
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/act_{adAccountId}/adcreatives";
                
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
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/act_{adAccountId}/ads";
                
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
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{adId}";
                
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
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{adId}?access_token={accessToken}";
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

        public async Task<bool> DeleteCampaignAsync(string adAccountId, string campaignId, string accessToken, string deleteStrategy = "DELETE_ANY", DateTime? beforeDate = null, int? objectCount = null)
        {
            try
            {
                // Use sandbox config if enabled
                if (_facebookSettings.UseSandbox && _facebookSettings.Sandbox != null)
                {
                    accessToken = _facebookSettings.Sandbox.AccessToken;
                    _logger.LogInformation("Using sandbox configuration for campaign deletion");
                }

                // Delete specific campaign by ID (correct Facebook API approach)
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{campaignId}?access_token={accessToken}";
                
                _logger.LogInformation("Deleting campaign {CampaignId} using direct campaign endpoint", campaignId);

                var response = await _httpClient.DeleteAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete campaign: {Error}", errorContent);
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Campaign deletion response: {Response}", responseContent);

                // Parse response to check if deletion was successful
                try
                {
                    var jsonResponse = JsonDocument.Parse(responseContent);
                    if (jsonResponse.RootElement.TryGetProperty("success", out var successProperty))
                    {
                        return successProperty.GetBoolean();
                    }
                    
                    // For direct campaign deletion, success is indicated by 200 status code
                    return true;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Could not parse deletion response, assuming success");
                    return true; // Assume success if we can't parse response but got 200 status
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting campaign {CampaignId}", campaignId);
                return false;
            }
        }

        public async Task<AdPerformanceResponse?> GetAdInsightsAsync(string adId, string accessToken)
        {
            try
            {
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{adId}/insights?fields=impressions,clicks,ctr,spend,actions&access_token={accessToken}";
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
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/me?access_token={accessToken}";
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

        /// <summary>
        /// Normalizes ad account ID by adding 'act_' prefix if missing
        /// </summary>
        /// <param name="adAccountId">Ad account ID with or without prefix</param>
        /// <returns>Normalized ad account ID with act_ prefix</returns>
        private static string NormalizeAdAccountId(string adAccountId)
        {
            if (string.IsNullOrEmpty(adAccountId)) 
                return adAccountId;
            
            return adAccountId.StartsWith("act_") ? adAccountId : $"act_{adAccountId}";
        }
    }
}
