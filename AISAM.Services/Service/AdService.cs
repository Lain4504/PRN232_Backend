using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.Helper;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class AdService : IAdService
    {
        private readonly IAdCampaignsRepository _campaigns;
        private readonly IAdSetsRepository _adSets;
        private readonly IAdCreativesRepository _creatives;
        private readonly IAdsRepository _ads;
        private readonly ISocialIntegrationsRepository _integrations;
        private readonly ISubscriptionsRepository _subscriptions;
        private readonly INotificationsRepository _notifications;
        private readonly TikTokAdsClient _tiktok;
        private readonly RolePermissionConfig _roleConfig;
        private readonly ILogger<AdService> _logger;

        public AdService(
            IAdCampaignsRepository campaigns,
            IAdSetsRepository adSets,
            IAdCreativesRepository creatives,
            IAdsRepository ads,
            ISocialIntegrationsRepository integrations,
            ISubscriptionsRepository subscriptions,
            INotificationsRepository notifications,
            TikTokAdsClient tiktok,
            RolePermissionConfig roleConfig,
            ILogger<AdService> logger)
        {
            _campaigns = campaigns;
            _adSets = adSets;
            _creatives = creatives;
            _ads = ads;
            _integrations = integrations;
            _subscriptions = subscriptions;
            _notifications = notifications;
            _tiktok = tiktok;
            _roleConfig = roleConfig;
            _logger = logger;
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new UnauthorizedAccessException(message);
        }

        private static void NotFoundIfNull(object? value, string message)
        {
            if (value == null) throw new KeyNotFoundException(message);
        }

        private static bool HasPermission(string role, string permission)
        {
            if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)) return true;
            // For MVP accept vendor/user and map minimal perms
            if (string.Equals(role, "vendor", StringComparison.OrdinalIgnoreCase)) return permission is "can_create_ad" or "can_publish_ad" or "can_update_ad";
            if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase)) return permission is "can_create_ad" or "can_publish_ad" or "can_update_ad";
            return false;
        }

        private async Task<(string accessToken, string advertiserId)> GetTikTokAuthAsync(Guid brandId, CancellationToken ct)
        {
            var si = await _integrations.GetActiveForBrandAsync(brandId, SocialPlatformEnum.TikTok, ct);
            NotFoundIfNull(si, "No TikTok integration for brand");
            Ensure(!string.IsNullOrWhiteSpace(si!.AccessToken), "Missing TikTok access token");
            Ensure(!string.IsNullOrWhiteSpace(si.ExternalId), "Missing TikTok advertiser/account id");
            if (si.ExpiresAt.HasValue && si.ExpiresAt.Value <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("TikTok access token expired");
            return (si.AccessToken, si.ExternalId!);
        }

        private async Task CheckQuotaAsync(Guid userId, CancellationToken ct)
        {
            var sub = await _subscriptions.GetActiveByUserAsync(userId, ct);
            if (sub == null || !sub.IsActive) throw new UnauthorizedAccessException("No active subscription");
            // Reuse posts quota as proxy for ads for MVP
            var count = await _subscriptions.CountAdsCreatedThisMonthAsync(userId, ct);
            if (count >= sub.QuotaPostsPerMonth) throw new InvalidOperationException("Quota exceeded for this month");
        }

        public async Task<CampaignResponse> CreateCampaignAsync(Guid userId, string role, CreateCampaignRequest request, CancellationToken ct)
        {
            Require(HasPermission(role, "can_create_ad"), "Forbidden");
            await CheckQuotaAsync(userId, ct);

            // Validate TikTok integration present
            var (token, advertiserId) = await GetTikTokAuthAsync(request.BrandId, ct);

            // Create local campaign
            var campaign = new AdCampaign
            {
                UserId = userId,
                BrandId = request.BrandId,
                AdAccountId = advertiserId,
                Name = request.Name,
                Objective = request.Objective,
                Budget = request.Budget,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            campaign = await _campaigns.AddAsync(campaign, ct);

            await _notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = "Ad campaign created",
                Message = $"Campaign {campaign.Name} created",
                Type = NotificationTypeEnum.SystemUpdate,
                TargetId = campaign.Id,
                TargetType = "ad_campaign",
                CreatedAt = DateTime.UtcNow
            }, ct);

            _logger.LogInformation("User {UserId} created campaign {CampaignId}", userId, campaign.Id);

            return new CampaignResponse
            {
                Id = campaign.Id,
                BrandId = campaign.BrandId,
                AdAccountId = campaign.AdAccountId,
                Name = campaign.Name,
                Objective = campaign.Objective,
                Budget = campaign.Budget,
                IsActive = campaign.IsActive,
                CreatedAt = campaign.CreatedAt
            };
        }

        public async Task<AdSetResponse> CreateAdSetAsync(Guid userId, string role, CreateAdSetRequest request, CancellationToken ct)
        {
            Require(HasPermission(role, "can_create_ad"), "Forbidden");

            var campaign = await _campaigns.GetByIdAsync(request.CampaignId, ct);
            NotFoundIfNull(campaign, "Campaign not found");
            Ensure(campaign!.UserId == userId || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase), "Forbidden");

            var adset = new AdSet
            {
                CampaignId = request.CampaignId,
                Name = request.Name,
                Targeting = request.TargetingJson,
                DailyBudget = request.DailyBudget,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CreatedAt = DateTime.UtcNow
            };
            adset = await _adSets.AddAsync(adset, ct);

            _logger.LogInformation("User {UserId} created adset {AdSetId}", userId, adset.Id);

            return new AdSetResponse
            {
                Id = adset.Id,
                CampaignId = adset.CampaignId,
                Name = adset.Name,
                TargetingJson = adset.Targeting,
                DailyBudget = adset.DailyBudget,
                CreatedAt = adset.CreatedAt
            };
        }

        public async Task<CreativeResponse> CreateCreativeAsync(Guid userId, string role, CreateCreativeRequest request, CancellationToken ct)
        {
            Require(HasPermission(role, "can_create_ad"), "Forbidden");

            var (token, advertiserId) = await GetTikTokAuthAsync(request.BrandId, ct);

            // For MVP, ensure content approved
            // We rely on db to ensure content exists and is approved via FK and prior workflow; skip heavy checks to keep scope

            var creative = new AdCreative
            {
                ContentId = request.ContentId,
                AdAccountId = advertiserId,
                CallToAction = request.CallToAction,
                CreatedAt = DateTime.UtcNow
            };
            creative = await _creatives.AddAsync(creative, ct);

            await _notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = "Ad creative created",
                Message = $"Creative for content {creative.ContentId} created",
                Type = NotificationTypeEnum.SystemUpdate,
                TargetId = creative.Id,
                TargetType = "ad_creative",
                CreatedAt = DateTime.UtcNow
            }, ct);

            return new CreativeResponse
            {
                Id = creative.Id,
                ContentId = creative.ContentId,
                AdAccountId = creative.AdAccountId,
                CreativeExternalId = creative.CreativeId,
                CallToAction = creative.CallToAction,
                CreatedAt = creative.CreatedAt
            };
        }

        public async Task<AdResponse> PublishAdAsync(Guid userId, string role, PublishAdRequest request, CancellationToken ct)
        {
            Require(HasPermission(role, "can_publish_ad"), "Forbidden");
            await CheckQuotaAsync(userId, ct);

            var adset = await _adSets.GetByIdAsync(request.AdSetId, ct);
            NotFoundIfNull(adset, "Ad set not found");

            var campaign = await _campaigns.GetByIdAsync(adset!.CampaignId, ct);
            NotFoundIfNull(campaign, "Campaign not found");
            Ensure(campaign!.UserId == userId || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase), "Forbidden");

            var creative = await _creatives.GetByIdAsync(request.CreativeId, ct);
            NotFoundIfNull(creative, "Creative not found");
            Ensure(string.Equals(creative!.AdAccountId, campaign.AdAccountId, StringComparison.OrdinalIgnoreCase), "Creative and campaign account mismatch");

            var (token, advertiserId) = await GetTikTokAuthAsync(campaign.BrandId, ct);

            // Minimal: call TikTok create ad; we won't parse response deeply for MVP
            var tiktokResponse = await _tiktok.CreateAdAsync(token, advertiserId, adset.Id.ToString(), creative.Id.ToString(), $"Ad-{creative.Id.ToString()[..6]}", ct);

            var ad = new Ad
            {
                AdSetId = adset.Id,
                CreativeId = creative.Id,
                AdId = null, // store external id when parsed later
                Status = "published",
                CreatedAt = DateTime.UtcNow
            };
            ad = await _ads.AddAsync(ad, ct);

            await _notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = "Ad published",
                Message = $"Ad {ad.Id} published",
                Type = NotificationTypeEnum.SystemUpdate,
                TargetId = ad.Id,
                TargetType = "ad",
                CreatedAt = DateTime.UtcNow
            }, ct);

            _logger.LogInformation("User {UserId} published ad {AdId}", userId, ad.Id);

            return new AdResponse
            {
                Id = ad.Id,
                AdSetId = ad.AdSetId,
                CreativeId = ad.CreativeId,
                ExternalAdId = ad.AdId,
                Status = ad.Status,
                CreatedAt = ad.CreatedAt
            };
        }

        public async Task<List<CampaignResponse>> GetCampaignsAsync(Guid? userId, Guid? brandId, string role, CancellationToken ct)
        {
            var list = await _campaigns.ListAsync(userId, brandId, ct);
            return list.Select(c => new CampaignResponse
            {
                Id = c.Id,
                BrandId = c.BrandId,
                AdAccountId = c.AdAccountId,
                Name = c.Name,
                Objective = c.Objective,
                Budget = c.Budget,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task<List<AdResponse>> GetAdsAsync(Guid? userId, Guid? brandId, Guid? campaignId, string role, CancellationToken ct)
        {
            var list = await _ads.ListAsync(userId, brandId, campaignId, ct);
            return list.Select(a => new AdResponse
            {
                Id = a.Id,
                AdSetId = a.AdSetId,
                CreativeId = a.CreativeId,
                ExternalAdId = a.AdId,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task<CampaignResponse> UpdateCampaignAsync(Guid userId, string role, Guid id, UpdateCampaignRequest request, CancellationToken ct)
        {
            Require(HasPermission(role, "can_update_ad"), "Forbidden");
            var c = await _campaigns.GetByIdAsync(id, ct);
            NotFoundIfNull(c, "Campaign not found");
            Ensure(c!.UserId == userId || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase), "Forbidden");

            if (request.Name != null) c.Name = request.Name;
            if (request.Objective != null) c.Objective = request.Objective;
            if (request.Budget.HasValue) c.Budget = request.Budget;
            if (request.IsActive.HasValue) c.IsActive = request.IsActive.Value;
            c.UpdatedAt = DateTime.UtcNow;
            await _campaigns.UpdateAsync(c, ct);

            return new CampaignResponse
            {
                Id = c.Id,
                BrandId = c.BrandId,
                AdAccountId = c.AdAccountId,
                Name = c.Name,
                Objective = c.Objective,
                Budget = c.Budget,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            };
        }

        public async Task<AdResponse> UpdateAdAsync(Guid userId, string role, Guid id, UpdateAdRequest request, CancellationToken ct)
        {
            Require(HasPermission(role, "can_update_ad"), "Forbidden");
            var ad = await _ads.GetByIdAsync(id, ct);
            NotFoundIfNull(ad, "Ad not found");

            // Ownership via campaign
            var adset = await _adSets.GetByIdAsync(ad!.AdSetId, ct);
            var campaign = adset != null ? await _campaigns.GetByIdAsync(adset.CampaignId, ct) : null;
            NotFoundIfNull(campaign, "Campaign not found");
            Ensure(campaign!.UserId == userId || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase), "Forbidden");

            // Update status locally and attempt remote if we had external id
            ad.Status = request.Status;
            await _ads.UpdateAsync(ad, ct);

            _logger.LogInformation("User {UserId} updated ad {AdId} to {Status}", userId, ad.Id, ad.Status);

            return new AdResponse
            {
                Id = ad.Id,
                AdSetId = ad.AdSetId,
                CreativeId = ad.CreativeId,
                ExternalAdId = ad.AdId,
                Status = ad.Status,
                CreatedAt = ad.CreatedAt
            };
        }

        public async Task DeleteCampaignAsync(Guid userId, string role, Guid id, CancellationToken ct)
        {
            var c = await _campaigns.GetByIdAsync(id, ct);
            NotFoundIfNull(c, "Campaign not found");
            Ensure(c!.UserId == userId || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase), "Forbidden");
            if (c.IsActive) throw new InvalidOperationException("Cannot delete running campaign");
            await _campaigns.DeleteAsync(c, ct);
            await _notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = "Ad campaign deleted",
                Message = $"Campaign {c.Name} deleted",
                Type = NotificationTypeEnum.SystemUpdate,
                TargetId = c.Id,
                TargetType = "ad_campaign"
            }, ct);
        }

        public async Task DeleteAdAsync(Guid userId, string role, Guid id, CancellationToken ct)
        {
            var ad = await _ads.GetByIdAsync(id, ct);
            NotFoundIfNull(ad, "Ad not found");
            var adset = ad != null ? await _adSets.GetByIdAsync(ad!.AdSetId, ct) : null;
            var campaign = adset != null ? await _campaigns.GetByIdAsync(adset!.CampaignId, ct) : null;
            NotFoundIfNull(campaign, "Campaign not found");
            Ensure(campaign!.UserId == userId || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase), "Forbidden");
            if (string.Equals(ad!.Status, "active", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot delete running ad");
            await _ads.SoftDeleteAsync(ad!, ct);
            await _notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = "Ad deleted",
                Message = $"Ad {ad.Id} deleted",
                Type = NotificationTypeEnum.SystemUpdate,
                TargetId = ad.Id,
                TargetType = "ad"
            }, ct);
        }

        public async Task PullReportsAsync(Guid userId, string role, Guid adId, CancellationToken ct)
        {
            var ad = await _ads.GetByIdAsync(adId, ct);
            NotFoundIfNull(ad, "Ad not found");
            var adset = await _adSets.GetByIdAsync(ad!.AdSetId, ct);
            var campaign = adset != null ? await _campaigns.GetByIdAsync(adset!.CampaignId, ct) : null;
            NotFoundIfNull(campaign, "Campaign not found");
            Ensure(campaign!.UserId == userId || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase), "Forbidden");

            var (token, advertiserId) = await GetTikTokAuthAsync(campaign.BrandId, ct);
            var end = DateTime.UtcNow.Date;
            var start = end.AddDays(-1);
            var reportJson = await _tiktok.GetAdReportAsync(token, advertiserId, ad.Id.ToString(), start, end, ct);
            _logger.LogInformation("Report for ad {AdId}: {Payload}", ad.Id, reportJson);
        }
    }
}


