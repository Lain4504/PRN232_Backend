using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AISAM.Services.Service
{
    public class TikTokAdsClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public TikTokAdsClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = Environment.GetEnvironmentVariable("TIKTOK_BASE_URL")
                       ?? configuration["TikTok:BaseUrl"]
                       ?? "https://business-api.tiktok.com/open_api/sandbox/v1.3";
        }

        private void ApplyAuth(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Access-Token", accessToken);
        }

        private static StringContent JsonBody(object payload)
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        public async Task<string> CreateCampaignAsync(string accessToken, string advertiserId, string name, string? objective, decimal? budget, CancellationToken ct)
        {
            ApplyAuth(accessToken);
            var url = $"{_baseUrl}/campaign/create/";
            var body = new
            {
                advertiser_id = advertiserId,
                campaign_name = name,
                objective_type = objective ?? "REACH",
                budget_mode = budget.HasValue ? "BUDGET_MODE_INFINITE" : "BUDGET_MODE_INFINITE",
                // For MVP we avoid budget at campaign if not required
            };
            var res = await _httpClient.PostAsync(url, JsonBody(body), ct);
            var content = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"TikTok campaign create failed: {content}");
            return content;
        }

        public async Task<string> CreateAdSetAsync(string accessToken, string advertiserId, string campaignId, string name, string? targetingJson, decimal? dailyBudget, DateTime? start, DateTime? end, CancellationToken ct)
        {
            ApplyAuth(accessToken);
            var url = $"{_baseUrl}/adgroup/create/";
            object? targeting = null;
            if (!string.IsNullOrWhiteSpace(targetingJson))
            {
                try { targeting = JsonSerializer.Deserialize<object>(targetingJson!); } catch { targeting = null; }
            }
            var body = new
            {
                advertiser_id = advertiserId,
                campaign_id = campaignId,
                adgroup_name = name,
                billing_event = "IMPRESSION",
                budget = dailyBudget,
                schedule_start_time = start?.ToString("yyyy-MM-dd HH:mm:ss"),
                schedule_end_time = end?.ToString("yyyy-MM-dd HH:mm:ss"),
                targeting
            };
            var res = await _httpClient.PostAsync(url, JsonBody(body), ct);
            var content = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"TikTok ad set create failed: {content}");
            return content;
        }

        public async Task<string> CreateCreativeAsync(string accessToken, string advertiserId, string creativeName, string adText, string? imageId, string? videoId, string? callToAction, CancellationToken ct)
        {
            ApplyAuth(accessToken);
            var url = $"{_baseUrl}/creative/create/";
            var body = new
            {
                advertiser_id = advertiserId,
                creative_name = creativeName,
                ad_text = adText,
                image_id = imageId,
                video_id = videoId,
                call_to_action = callToAction
            };
            var res = await _httpClient.PostAsync(url, JsonBody(body), ct);
            var content = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"TikTok creative create failed: {content}");
            return content;
        }

        public async Task<string> CreateAdAsync(string accessToken, string advertiserId, string adGroupId, string creativeId, string adName, CancellationToken ct)
        {
            ApplyAuth(accessToken);
            var url = $"{_baseUrl}/ad/create/";
            var body = new
            {
                advertiser_id = advertiserId,
                adgroup_id = adGroupId,
                creative_id = creativeId,
                ad_name = adName
            };
            var res = await _httpClient.PostAsync(url, JsonBody(body), ct);
            var content = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"TikTok ad create failed: {content}");
            return content;
        }

        public async Task<string> UpdateStatusAsync(string accessToken, string advertiserId, string entity, string id, string status, CancellationToken ct)
        {
            ApplyAuth(accessToken);
            var url = $"{_baseUrl}/{entity}/update/status/";
            var body = new { advertiser_id = advertiserId, ids = new[] { id }, operation_status = status };
            var res = await _httpClient.PostAsync(url, JsonBody(body), ct);
            var content = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"TikTok {entity} status update failed: {content}");
            return content;
        }

        public async Task<string> GetAdReportAsync(string accessToken, string advertiserId, string adId, DateTime start, DateTime end, CancellationToken ct)
        {
            ApplyAuth(accessToken);
            var url = $"{_baseUrl}/report/integrated/get/";
            var body = new
            {
                advertiser_id = advertiserId,
                service_type = "AUCTION",
                report_type = "BASIC",
                data_level = "AUCTION_AD",
                dimensions = new[] { "stat_time_day" },
                metrics = new[] { "impressions", "clicks", "ctr" },
                start_date = start.ToString("yyyy-MM-dd"),
                end_date = end.ToString("yyyy-MM-dd"),
                filtering = new { ad_ids = new[] { adId } }
            };
            var res = await _httpClient.PostAsync(url, JsonBody(body), ct);
            var content = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"TikTok report failed: {content}");
            return content;
        }
    }
}


