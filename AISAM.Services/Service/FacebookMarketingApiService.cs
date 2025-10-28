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
                // Get sandbox config if enabled
                var (finalAccessToken, finalAdAccountId, _) = GetSandboxConfig(accessToken, adAccountId);
                
                // Map objective to Facebook API value
                var mappedObjective = FacebookObjectiveMapper.MapToFacebookObjective(objective);
                _logger.LogInformation("Mapped objective '{Objective}' to '{MappedObjective}'", objective, mappedObjective);
                
                // Convert budget to appropriate currency unit
                var budgetInCurrencyUnit = ConvertBudgetToCurrencyUnit(budget);
                
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{finalAdAccountId}/campaigns";
                
                var payload = new
                {
                    name = name,
                    objective = mappedObjective,
                    status = "PAUSED",
                    buying_type = "AUCTION",
                    daily_budget = budgetInCurrencyUnit, // VND for sandbox, cents for production
                    special_ad_categories = new string[] { }, // Empty array, not ["NONE"]
                    access_token = finalAccessToken
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

        public async Task<string> CreateAdSetAsync(string adAccountId, string campaignId, string name, string targeting, decimal dailyBudget, string accessToken, DateTime? startTime = null, DateTime? endTime = null, string? optimizationGoal = null, string? billingEvent = null)
        {
            try
            {
                // Get sandbox config if enabled
                var (finalAccessToken, finalAdAccountId, _) = GetSandboxConfig(accessToken, adAccountId);

                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{finalAdAccountId}/adsets";
                
                // Validate and parse targeting JSON
                object targetingObj;
                try
                {
                    targetingObj = JsonSerializer.Deserialize<object>(targeting) ?? new { };
                    
                    // Add targeting_automation with advantage_audience if not present
                    if (targetingObj is JsonElement jsonElement)
                    {
                        var targetingDict = new Dictionary<string, object>();
                        
                        // Copy existing targeting properties
                        foreach (var property in jsonElement.EnumerateObject())
                        {
                            targetingDict[property.Name] = property.Value;
                        }
                        
                        // Add targeting_automation if not present
                        if (!targetingDict.ContainsKey("targeting_automation"))
                        {
                            targetingDict["targeting_automation"] = new Dictionary<string, object>
                            {
                                ["advantage_audience"] = 1 // Enable Advantage Audience by default
                            };
                            _logger.LogInformation("Added targeting_automation with advantage_audience=1 to targeting");
                        }
                        
                        // Adjust age_max for Advantage Audience (must be >= 65)
                        if (targetingDict.ContainsKey("age_max"))
                        {
                            var currentAgeMax = targetingDict["age_max"];
                            int ageMaxValue = 0;
                            
                            // Parse age_max value
                            if (currentAgeMax is JsonElement ageElement)
                            {
                                ageMaxValue = ageElement.ValueKind == JsonValueKind.Number ? 
                                    ageElement.GetInt32() : 
                                    (int.TryParse(ageElement.GetString(), out var parsed) ? parsed : 0);
                            }
                            else if (currentAgeMax is int intValue)
                            {
                                ageMaxValue = intValue;
                            }
                            
                            // Adjust if below 65
                            if (ageMaxValue > 0 && ageMaxValue < 65)
                            {
                                targetingDict["age_max"] = 65;
                                _logger.LogInformation("Adjusted age_max from {OldAge} to 65 for Advantage Audience compatibility", ageMaxValue);
                            }
                        }
                        else
                        {
                            // Set default age_max to 65 if not specified
                            targetingDict["age_max"] = 65;
                            _logger.LogInformation("Set default age_max to 65 for Advantage Audience compatibility");
                        }
                        
                        targetingObj = targetingDict;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Invalid targeting JSON format: {Targeting}", targeting);
                    throw new ArgumentException("Invalid targeting JSON format", ex);
                }
                
                // Convert budget to appropriate currency unit
                var budgetInCurrencyUnit = ConvertBudgetToCurrencyUnit(dailyBudget);

                // Check if campaign has budget set - if so, don't set daily_budget on ad set
                var campaignHasBudget = await CheckCampaignBudgetAsync(campaignId, finalAccessToken);
                _logger.LogInformation("Campaign {CampaignId} has budget: {HasBudget}", campaignId, campaignHasBudget);

                // Build payload according to Facebook Marketing API v23.0
                var payload = new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["campaign_id"] = campaignId,
                    ["targeting"] = targetingObj,
                    ["status"] = "PAUSED",
                    ["access_token"] = finalAccessToken
                };

                // Only add daily_budget if campaign doesn't have budget set
                if (!campaignHasBudget)
                {
                    payload["daily_budget"] = budgetInCurrencyUnit; // VND for sandbox, cents for production
                    _logger.LogInformation("Adding daily_budget {Budget} to Ad Set because Campaign has no budget", budgetInCurrencyUnit);
                }
                else
                {
                    _logger.LogInformation("Skipping daily_budget for Ad Set because Campaign already has budget");
                }

                // Add optional parameters if provided
                if (startTime.HasValue)
                {
                    payload["start_time"] = startTime.Value.ToString("yyyy-MM-ddTHH:mm:sszzz");
                }

                if (endTime.HasValue)
                {
                    payload["end_time"] = endTime.Value.ToString("yyyy-MM-ddTHH:mm:sszzz");
                }

                if (!string.IsNullOrEmpty(optimizationGoal))
                {
                    payload["optimization_goal"] = optimizationGoal;
                }

                if (!string.IsNullOrEmpty(billingEvent))
                {
                    payload["billing_event"] = billingEvent;
                }

                // Add default optimization goal for sandbox mode
                if (_facebookSettings.UseSandbox && !payload.ContainsKey("optimization_goal"))
                {
                    payload["optimization_goal"] = "REACH"; // Default for sandbox
                }

                // Add bid_amount for optimization goals that require it
                if (payload.ContainsKey("optimization_goal"))
                {
                    var currentOptimizationGoal = payload["optimization_goal"].ToString();
                    var currentBillingEvent = payload.ContainsKey("billing_event") ? payload["billing_event"].ToString() : "";
                    
                    // REACH with IMPRESSIONS billing requires bid_amount
                    if (currentOptimizationGoal == "REACH" && currentBillingEvent == "IMPRESSIONS")
                    {
                        var bidAmount = (int)(budgetInCurrencyUnit * 0.1);
                        payload["bid_amount"] = Math.Max(bidAmount, 100); // Minimum 100 VND
                    }
                    // LINK_CLICKS with IMPRESSIONS billing requires bid_amount
                    else if (currentOptimizationGoal == "LINK_CLICKS" && currentBillingEvent == "IMPRESSIONS")
                    {
                        var bidAmount = (int)(budgetInCurrencyUnit * 0.05);
                        payload["bid_amount"] = Math.Max(bidAmount, 50); // Minimum 50 VND
                    }
                    // CONVERSIONS with IMPRESSIONS billing requires bid_amount
                    else if (currentOptimizationGoal == "CONVERSIONS" && currentBillingEvent == "IMPRESSIONS")
                    {
                        var bidAmount = (int)(budgetInCurrencyUnit * 0.15);
                        payload["bid_amount"] = Math.Max(bidAmount, 200); // Minimum 200 VND
                    }
                }

                _logger.LogInformation("Creating ad set with payload: {Payload}", JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create ad set: {Error}", errorContent);
                    throw new Exception($"Facebook API Error: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Ad set creation response: {Response}", responseContent);
                
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                if (jsonResponse.RootElement.TryGetProperty("id", out var idProperty))
                {
                    var adSetId = idProperty.GetString();
                    _logger.LogInformation("Successfully created ad set {AdSetId} for campaign {CampaignId}", adSetId, campaignId);
                    return adSetId ?? throw new Exception("Ad Set ID not found in response");
                }

                throw new Exception("Invalid response from Facebook API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad set for campaign {CampaignId}", campaignId);
                throw;
            }
        }

        /// <summary>
        /// Get the latest published post ID from a Facebook page
        /// </summary>
        public async Task<string> GetLatestPostIdAsync(string pageId, string accessToken)
        {
            try
            {
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{pageId}/posts?limit=1&access_token={accessToken}";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get latest post: {Error}", errorContent);
                    throw new Exception($"Facebook API Error: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                if (jsonResponse.RootElement.TryGetProperty("data", out var dataProperty) && 
                    dataProperty.GetArrayLength() > 0)
                {
                    var firstPost = dataProperty[0];
                    if (firstPost.TryGetProperty("id", out var idProperty))
                    {
                        return idProperty.GetString() ?? "";
                    }
                }
                
                throw new Exception("No published posts found on the page");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest post ID for page {PageId}", pageId);
                throw;
            }
        }

        public async Task<string> CreateAdCreativeAsync(string adAccountId, string pageId, string message, string? imageUrl, string? videoUrl, string? callToAction, string accessToken, string? linkUrl = null, string? adName = null)
        {
            try
            {
                // Get sandbox config for development mode
                var (sandboxAccessToken, sandboxAdAccountId, sandboxPageId) = GetSandboxConfig(accessToken, adAccountId, pageId);
                
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{sandboxAdAccountId}/adcreatives";
                
                object objectStorySpec;
                
                // Determine creative type and build appropriate object_story_spec
                if (!string.IsNullOrEmpty(videoUrl))
                {
                    // Video creative
                    objectStorySpec = new
                    {
                        page_id = sandboxPageId,
                        video_data = new
                        {
                            video_id = videoUrl,
                            message = message,
                            call_to_action = !string.IsNullOrEmpty(callToAction) ? new 
                            { 
                                type = callToAction,
                                value = !string.IsNullOrEmpty(linkUrl) ? new { link = linkUrl } : null
                            } : null
                        }
                    };
                }
                else
                {
                    // Image/Link creative
                    objectStorySpec = new
                    {
                        page_id = sandboxPageId,
                        link_data = new
                        {
                            link = linkUrl ?? "https://www.facebook.com", // Default link if none provided
                            message = message,
                            picture = imageUrl,
                            name = adName ?? "Ad Creative", // Required field
                            call_to_action = !string.IsNullOrEmpty(callToAction) ? new 
                            { 
                                type = callToAction,
                                value = !string.IsNullOrEmpty(linkUrl) ? new { link = linkUrl } : null
                            } : null
                        }
                    };
                }

                // For sandbox mode, we need to use an existing post ID instead of creating new content
                // Get the latest published post ID from the page using the original access token (page access token)
                // Use original pageId instead of sandboxPageId to get posts from the actual page
                var latestPostId = await GetLatestPostIdAsync(pageId, accessToken);
                
                var payload = new
                {
                    name = adName ?? "Ad Creative", // Required field for creative
                    object_story_id = latestPostId, // Use existing published post ID
                    access_token = sandboxAccessToken
                };

                _logger.LogInformation("Creating ad creative with payload: {Payload}", JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create ad creative: {Error}", errorContent);
                    throw new Exception($"Facebook API Error: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Ad creative creation response: {Response}", responseContent);
                
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                if (jsonResponse.RootElement.TryGetProperty("id", out var idProperty))
                {
                    var creativeId = idProperty.GetString();
                    _logger.LogInformation("Successfully created ad creative {CreativeId}", creativeId);
                    return creativeId ?? throw new Exception("Creative ID not found in response");
                }

                throw new Exception("Invalid response from Facebook API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad creative for page {PageId}", pageId);
                throw;
            }
        }

        public async Task<string> CreateAdAsync(string adAccountId, string facebookAdSetId, string facebookCreativeId, string status, string accessToken, string? adName = null)
        {
            try
            {
                // Get sandbox config for development mode
                var (sandboxAccessToken, sandboxAdAccountId, _) = GetSandboxConfig(accessToken, adAccountId);
                
                // Validate that adSetId is a numeric Facebook ID, not a GUID
                if (!long.TryParse(facebookAdSetId, out _))
                {
                    throw new ArgumentException("Ad Set ID must be a valid Facebook Ad Set ID (numeric string)");
                }

                // Validate that creativeId is a numeric Facebook ID, not a GUID
                if (!long.TryParse(facebookCreativeId, out _))
                {
                    throw new ArgumentException("Creative ID must be a valid Facebook Creative ID (numeric string)");
                }

                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{sandboxAdAccountId}/ads";
                
                var payload = new
                {
                    name = adName ?? $"Ad for AdSet {facebookAdSetId}",
                    adset_id = facebookAdSetId,
                    creative = new { creative_id = facebookCreativeId },
                    status = status,
                    access_token = sandboxAccessToken
                };

                _logger.LogInformation("Creating ad with payload: {Payload}", JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create ad: {Error}", errorContent);
                    throw new Exception($"Facebook API Error: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Ad creation response: {Response}", responseContent);
                
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                if (jsonResponse.RootElement.TryGetProperty("id", out var idProperty))
                {
                    var adId = idProperty.GetString();
                    _logger.LogInformation("Successfully created ad {AdId} for ad set {AdSetId}", adId, facebookAdSetId);
                    return adId ?? throw new Exception("Ad ID not found in response");
                }

                throw new Exception("Invalid response from Facebook API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad for ad set {AdSetId}", facebookAdSetId);
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

        public async Task<bool> UpdateAdSetStatusAsync(string adSetId, string status, string accessToken)
        {
            try
            {
                // Get sandbox config for development mode
                var (sandboxAccessToken, _, _) = GetSandboxConfig(accessToken, "");
                
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{adSetId}";
                
                var payload = new
                {
                    status = status,
                    access_token = sandboxAccessToken
                };

                _logger.LogInformation("Updating ad set {AdSetId} status to {Status}", adSetId, status);

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update ad set status: {Error}", errorContent);
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Ad set status update response: {Response}", responseContent);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ad set status for ad set {AdSetId}", adSetId);
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

        public async Task<bool> DeleteAdSetAsync(string adSetId, string accessToken)
        {
            try
            {
                // Use sandbox config if enabled
                if (_facebookSettings.UseSandbox && _facebookSettings.Sandbox != null)
                {
                    accessToken = _facebookSettings.Sandbox.AccessToken;
                    _logger.LogInformation("Using sandbox configuration for ad set deletion");
                }

                // Delete specific ad set by ID (correct Facebook API approach)
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{adSetId}?access_token={accessToken}";
                
                _logger.LogInformation("Deleting ad set {AdSetId} using direct ad set endpoint", adSetId);

                var response = await _httpClient.DeleteAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete ad set: {Error}", errorContent);
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Ad set deletion response: {Response}", responseContent);

                // Parse response to check if deletion was successful
                try
                {
                    var jsonResponse = JsonDocument.Parse(responseContent);
                    if (jsonResponse.RootElement.TryGetProperty("success", out var successProperty))
                    {
                        return successProperty.GetBoolean();
                    }
                    
                    // For direct ad set deletion, success is indicated by 200 status code
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
                _logger.LogError(ex, "Error deleting ad set {AdSetId}", adSetId);
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

        // https://developers.facebook.com/docs/marketing-api/generatepreview/v24.0
        public async Task<string?> GetAdCreativePreviewHtmlAsync(string creativeId, string adFormat, string accessToken)
        {
            try
            {
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{creativeId}/previews";
                var parameters = new Dictionary<string, string>
                {
                    ["ad_format"] = adFormat,
                    ["access_token"] = accessToken
                };
                var query = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var fullUrl = $"{url}?{query}";

                var response = await _httpClient.GetAsync(fullUrl);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get creative preview {Status}: {Body}", response.StatusCode, content);
                    return null;
                }

                using var doc = JsonDocument.Parse(content);
                var data = doc.RootElement.GetProperty("data");
                if (data.GetArrayLength() == 0) return null;
                var body = data[0].GetProperty("body").GetString();
                return body; // iframe HTML
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ad creative preview for {CreativeId}", creativeId);
                return null;
            }
        }

        public async Task<string?> GeneratePreviewBySpecAsync(string adAccountId, object creativeSpec, string adFormat, string accessToken)
        {
            try
            {
                var (finalAccessToken, finalAdAccountId, _) = GetSandboxConfig(accessToken, adAccountId);
                var actId = NormalizeAdAccountId(finalAdAccountId);
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{actId}/generatepreviews";

                var payload = new Dictionary<string, object>
                {
                    ["creative"] = creativeSpec,
                    ["ad_format"] = adFormat,
                    ["access_token"] = finalAccessToken
                };

                var json = JsonSerializer.Serialize(payload);
                var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to generate preview by spec {Status}: {Body}", response.StatusCode, content);
                    return null;
                }

                using var doc = JsonDocument.Parse(content);
                var data = doc.RootElement.GetProperty("data");
                if (data.GetArrayLength() == 0) return null;
                var body = data[0].GetProperty("body").GetString();
                return body;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview by spec for ad account {AdAccountId}", adAccountId);
                return null;
            }
        }

        public async Task<string?> GetAdPreviewHtmlAsync(string adId, string adFormat, string accessToken)
        {
            try
            {
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{adId}/previews";
                var parameters = new Dictionary<string, string>
                {
                    ["ad_format"] = adFormat,
                    ["access_token"] = accessToken
                };
                var query = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var fullUrl = $"{url}?{query}";

                var response = await _httpClient.GetAsync(fullUrl);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get ad preview {Status}: {Body}", response.StatusCode, content);
                    return null;
                }

                using var doc = JsonDocument.Parse(content);
                var data = doc.RootElement.GetProperty("data");
                if (data.GetArrayLength() == 0) return null;
                var body = data[0].GetProperty("body").GetString();
                return body;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ad preview for {AdId}", adId);
                return null;
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

        /// <summary>
        /// Check if campaign has budget set
        /// </summary>
        /// <param name="campaignId">Campaign ID</param>
        /// <param name="accessToken">Access token</param>
        /// <returns>True if campaign has budget, false otherwise</returns>
        private async Task<bool> CheckCampaignBudgetAsync(string campaignId, string accessToken)
        {
            try
            {
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{campaignId}?fields=daily_budget,lifetime_budget&access_token={accessToken}";
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to check campaign budget for {CampaignId}, assuming no budget", campaignId);
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Campaign budget API response: {Response}", responseContent);
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                // Check if campaign has daily_budget or lifetime_budget
                var hasDailyBudget = jsonResponse.RootElement.TryGetProperty("daily_budget", out var dailyBudget) && 
                    (dailyBudget.ValueKind == JsonValueKind.String ? 
                        (int.TryParse(dailyBudget.GetString(), out var dailyBudgetValue) && dailyBudgetValue > 0) :
                        (dailyBudget.ValueKind == JsonValueKind.Number && dailyBudget.GetInt32() > 0));
                        
                var hasLifetimeBudget = jsonResponse.RootElement.TryGetProperty("lifetime_budget", out var lifetimeBudget) && 
                    (lifetimeBudget.ValueKind == JsonValueKind.String ? 
                        (int.TryParse(lifetimeBudget.GetString(), out var lifetimeBudgetValue) && lifetimeBudgetValue > 0) :
                        (lifetimeBudget.ValueKind == JsonValueKind.Number && lifetimeBudget.GetInt32() > 0));
                
                var hasBudget = hasDailyBudget || hasLifetimeBudget;
                _logger.LogInformation("Campaign {CampaignId} budget check: Daily={DailyBudget}, Lifetime={LifetimeBudget}, HasBudget={HasBudget}", 
                    campaignId, hasDailyBudget, hasLifetimeBudget, hasBudget);
                
                return hasBudget;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking campaign budget for {CampaignId}, assuming no budget", campaignId);
                return false;
            }
        }

        /// <summary>
        /// Gets the appropriate access token and ad account ID based on sandbox mode
        /// </summary>
        /// <param name="originalAccessToken">Original access token</param>
        /// <param name="originalAdAccountId">Original ad account ID</param>
        /// <returns>Tuple of (accessToken, adAccountId)</returns>
        private (string accessToken, string adAccountId, string pageId) GetSandboxConfig(string originalAccessToken, string originalAdAccountId, string originalPageId = "")
        {
            if (_facebookSettings.UseSandbox && _facebookSettings.Sandbox != null)
            {
                _logger.LogInformation("Using sandbox configuration - Currency: {Currency}, Timezone: {Timezone}, PageId: {PageId}", 
                    _facebookSettings.Sandbox.Currency, _facebookSettings.Sandbox.Timezone, _facebookSettings.Sandbox.PageId);
                
                return (_facebookSettings.Sandbox.AccessToken, NormalizeAdAccountId(_facebookSettings.Sandbox.AdAccountId), _facebookSettings.Sandbox.PageId);
            }
            
            return (originalAccessToken, NormalizeAdAccountId(originalAdAccountId), originalPageId);
        }

        /// <summary>
        /// Converts budget to appropriate currency unit based on sandbox mode
        /// </summary>
        /// <param name="budget">Budget amount</param>
        /// <returns>Budget in appropriate currency unit</returns>
        private int ConvertBudgetToCurrencyUnit(decimal budget)
        {
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
            
            return budgetInCurrencyUnit;
        }

        public async Task<FacebookPostDetails?> GetPostDetailsAsync(string postId, string accessToken)
        {
            try
            {
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{postId}";
                var parameters = new Dictionary<string, string>
                {
                    ["fields"] = "id,message,created_time,type,full_picture,permalink_url",
                    ["access_token"] = accessToken
                };

                var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var fullUrl = $"{url}?{queryString}";

                _logger.LogInformation("Getting Facebook post details for post {PostId}", postId);

                var response = await _httpClient.GetAsync(fullUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get Facebook post details: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return null;
                }

                var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("error", out var errorElement))
                {
                    _logger.LogError("Facebook API error getting post details: {Error}", errorElement.GetRawText());
                    return null;
                }

                var postDetails = new FacebookPostDetails
                {
                    Id = root.GetProperty("id").GetString() ?? postId,
                    Message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null,
                    CreatedTime = root.TryGetProperty("created_time", out var createdTimeElement) ? 
                        DateTime.Parse(createdTimeElement.GetString()!) : DateTime.MinValue,
                    Type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null,
                    ImageUrl = root.TryGetProperty("full_picture", out var pictureElement) ? pictureElement.GetString() : null,
                    LinkUrl = root.TryGetProperty("permalink_url", out var permalinkElement) ? permalinkElement.GetString() : null
                };

                // For video posts, we might need to make an additional call to get video details
                if (postDetails.Type == "video" && !string.IsNullOrEmpty(postDetails.Id))
                {
                    try
                    {
                        var videoUrl = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{postDetails.Id}";
                        var videoParams = new Dictionary<string, string>
                        {
                            ["fields"] = "source",
                            ["access_token"] = accessToken
                        };
                        
                        var videoQueryString = string.Join("&", videoParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                        var videoFullUrl = $"{videoUrl}?{videoQueryString}";
                        
                        var videoResponse = await _httpClient.GetAsync(videoFullUrl);
                        if (videoResponse.IsSuccessStatusCode)
                        {
                            var videoContent = await videoResponse.Content.ReadAsStringAsync();
                            var videoDoc = JsonDocument.Parse(videoContent);
                            var videoRoot = videoDoc.RootElement;
                            
                            if (videoRoot.TryGetProperty("source", out var sourceElement))
                            {
                                postDetails.VideoUrl = sourceElement.GetString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get video source for post {PostId}", postDetails.Id);
                    }
                }

                _logger.LogInformation("Successfully retrieved Facebook post details for post {PostId}", postId);
                return postDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Facebook post details for post {PostId}", postId);
                return null;
            }
        }

        public async Task<string> CreateAdCreativeFromPostAsync(string adAccountId, string postId, string accessToken, string? adName = null)
        {
            try
            {
                // Get sandbox config if enabled
                var (finalAccessToken, finalAdAccountId, _) = GetSandboxConfig(accessToken, adAccountId);
                
                var url = $"{_facebookSettings.BaseUrl}/{_facebookSettings.GraphApiVersion}/{finalAdAccountId}/adcreatives";
                
                var payload = new
                {
                    name = adName ?? $"Ad Creative from Post {postId}",
                    object_story_id = postId, // Use existing post ID directly
                    access_token = finalAccessToken
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Creating ad creative from existing post {PostId} for account {AdAccountId}", postId, finalAdAccountId);

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create ad creative from post: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new Exception($"Facebook API Error: {responseContent}");
                }

                var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("error", out var errorElement))
                {
                    var errorMessage = errorElement.GetRawText();
                    _logger.LogError("Facebook API error creating ad creative from post: {Error}", errorMessage);
                    throw new Exception($"Facebook API Error: {errorMessage}");
                }

                if (!root.TryGetProperty("id", out var idElement))
                {
                    throw new Exception("Facebook API did not return creative ID");
                }

                var creativeId = idElement.GetString();
                if (string.IsNullOrEmpty(creativeId))
                {
                    throw new Exception("Facebook API returned empty creative ID");
                }

                _logger.LogInformation("Successfully created ad creative {CreativeId} from post {PostId}", creativeId, postId);
                return creativeId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad creative from post {PostId}", postId);
                throw;
            }
        }
    }
}
