using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class AdService : IAdService
    {
        private readonly IAdRepository _adRepository;
        private readonly IAdSetRepository _adSetRepository;
        private readonly IAdCreativeRepository _adCreativeRepository;
        private readonly IAdCampaignRepository _adCampaignRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IPerformanceReportRepository _performanceReportRepository;
        private readonly IAdQuotaService _adQuotaService;
        private readonly IFacebookMarketingApiService _facebookApiService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdService> _logger;

        public AdService(
            IAdRepository adRepository,
            IAdSetRepository adSetRepository,
            IAdCreativeRepository adCreativeRepository,
            IAdCampaignRepository adCampaignRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IPerformanceReportRepository performanceReportRepository,
            IAdQuotaService adQuotaService,
            IFacebookMarketingApiService facebookApiService,
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            ILogger<AdService> logger)
        {
            _adRepository = adRepository;
            _adSetRepository = adSetRepository;
            _adCreativeRepository = adCreativeRepository;
            _adCampaignRepository = adCampaignRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _performanceReportRepository = performanceReportRepository;
            _adQuotaService = adQuotaService;
            _facebookApiService = facebookApiService;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<AdResponse> CreateAdAsync(Guid userId, CreateAdRequest request)
        {
            try
            {
                // Validate ad set exists and user has access
                var adSet = await _adSetRepository.GetByIdWithDetailsAsync(request.AdSetId);
                if (adSet == null)
                {
                    throw new ArgumentException("Ad set not found");
                }

                await ValidateAdSetAccessAsync(userId, adSet);

                // Validate creative exists and user has access
                var creative = await _adCreativeRepository.GetByIdWithDetailsAsync(request.CreativeId);
                if (creative == null)
                {
                    throw new ArgumentException("Ad creative not found");
                }

                await ValidateCreativeAccessAsync(userId, creative);

                // Check quota if creating active ad
                if (request.Status?.ToUpper() == "ACTIVE")
                {
                    var quotaValidation = await _adQuotaService.ValidateQuotaAsync(userId, adSet.DailyBudget ?? 0);
                    if (!quotaValidation.canCreate)
                    {
                        throw new UnauthorizedAccessException(quotaValidation.errorMessage ?? "Quota exceeded");
                    }
                }

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(adSet.Campaign.BrandId);
                if (socialIntegration == null || !socialIntegration.IsActive || string.IsNullOrEmpty(socialIntegration.AdAccountId))
                {
                    throw new ArgumentException("No active social integration found for this brand");
                }

                // Check token validity
                var isTokenValid = await _facebookApiService.CheckTokenExpiryAsync(socialIntegration.AccessToken);
                if (!isTokenValid)
                {
                    throw new UnauthorizedAccessException("Facebook access token has expired. Please reconnect your account.");
                }

                // Create ad on Facebook
                var facebookAdId = await _facebookApiService.CreateAdAsync(
                    socialIntegration.AdAccountId,
                    adSet.Id.ToString(), // Using ad set ID as reference
                    creative.CreativeId!,
                    request.Status,
                    socialIntegration.AccessToken);

                // Save to database
                var ad = new Ad
                {
                    AdSetId = request.AdSetId,
                    CreativeId = request.CreativeId,
                    AdId = facebookAdId,
                    Status = request.Status
                };

                var createdAd = await _adRepository.CreateAsync(ad);

                // Send notification
                var notificationTitle = request.Status?.ToUpper() == "ACTIVE" ? "Ad Running" : "Ad Created";
                var notificationMessage = request.Status?.ToUpper() == "ACTIVE" 
                    ? $"Your ad is now running with ad set '{adSet.Name}'."
                    : $"Your ad has been created successfully with ad set '{adSet.Name}'.";

                await SendNotificationAsync(userId, notificationTitle, notificationMessage, createdAd.Id, "ad");

                _logger.LogInformation("User {UserId} created ad {AdId} with Facebook ID {FacebookAdId}", 
                    userId, createdAd.Id, facebookAdId);

                return MapToResponse(createdAd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad for user {UserId}", userId);
                throw;
            }
        }

        public async Task<PagedResult<AdResponse>> GetAdsAsync(Guid userId, Guid? campaignId, Guid? brandId, int page = 1, int pageSize = 20)
        {
            try
            {
                PagedResult<Ad> ads;

                if (campaignId.HasValue)
                {
                    // Validate campaign access
                    var campaign = await _adCampaignRepository.GetByIdAsync(campaignId.Value);
                    if (campaign == null)
                    {
                        throw new ArgumentException("Campaign not found");
                    }

                    await ValidateCampaignAccessAsync(userId, campaign);
                    ads = await _adRepository.GetByCampaignIdAsync(campaignId.Value, page, pageSize);
                }
                else if (brandId.HasValue)
                {
                    // Validate brand access
                    var brand = await GetBrandByIdAsync(brandId.Value);
                    if (brand == null)
                    {
                        throw new ArgumentException("Brand not found");
                    }

                    await ValidateBrandAccessAsync(userId, brand);
                    ads = await _adRepository.GetByBrandIdAsync(brandId.Value, page, pageSize);
                }
                else
                {
                    // Get all ads for user (including team brands if vendor)
                    ads = await GetUserAdsAsync(userId, page, pageSize);
                }

                var responses = ads.Data.Select(MapToResponse).ToList();
                
                return new PagedResult<AdResponse>
                {
                    Data = responses,
                    TotalCount = ads.TotalCount,
                    Page = ads.Page,
                    PageSize = ads.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ads for user {UserId}", userId);
                throw;
            }
        }

        public async Task<AdResponse?> GetAdByIdAsync(Guid userId, Guid adId)
        {
            try
            {
                var ad = await _adRepository.GetByIdWithDetailsAsync(adId);
                if (ad == null) return null;

                // Validate access through ad set and campaign
                await ValidateAdAccessAsync(userId, ad);

                return MapToResponse(ad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad {AdId} for user {UserId}", adId, userId);
                throw;
            }
        }

        public async Task<bool> UpdateAdStatusAsync(Guid userId, UpdateAdStatusRequest request)
        {
            try
            {
                var ad = await _adRepository.GetByIdWithDetailsAsync(request.AdId);
                if (ad == null)
                {
                    throw new ArgumentException("Ad not found");
                }

                // Validate access
                await ValidateAdAccessAsync(userId, ad);

                // Validate status
                var validStatuses = new[] { "ACTIVE", "PAUSED" };
                if (!validStatuses.Contains(request.Status.ToUpper()))
                {
                    throw new ArgumentException($"Invalid status. Valid options: {string.Join(", ", validStatuses)}");
                }

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(ad.AdSet.Campaign.BrandId);
                if (socialIntegration == null || !socialIntegration.IsActive)
                {
                    throw new ArgumentException("No active social integration found");
                }

                // Check token validity
                var isTokenValid = await _facebookApiService.CheckTokenExpiryAsync(socialIntegration.AccessToken);
                if (!isTokenValid)
                {
                    throw new UnauthorizedAccessException("Facebook access token has expired. Please reconnect your account.");
                }

                // Update status on Facebook
                var facebookSuccess = await _facebookApiService.UpdateAdStatusAsync(ad.AdId!, request.Status, socialIntegration.AccessToken);
                if (!facebookSuccess)
                {
                    throw new Exception("Failed to update ad status on Facebook");
                }

                // Update status in database
                var updated = await _adRepository.UpdateStatusAsync(ad.Id, request.Status);
                if (updated)
                {
                    // Send notification
                    var notificationTitle = request.Status.ToUpper() == "ACTIVE" ? "Ad Resumed" : "Ad Paused";
                    var notificationMessage = request.Status.ToUpper() == "ACTIVE" 
                        ? $"Your ad has been resumed and is now running."
                        : $"Your ad has been paused.";

                    await SendNotificationAsync(userId, notificationTitle, notificationMessage, ad.Id, "ad");

                    _logger.LogInformation("User {UserId} updated ad {AdId} status to {Status}", userId, ad.Id, request.Status);
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ad status for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteAdAsync(Guid userId, Guid adId)
        {
            try
            {
                var ad = await _adRepository.GetByIdWithDetailsAsync(adId);
                if (ad == null)
                {
                    return false;
                }

                // Validate access
                await ValidateAdAccessAsync(userId, ad);

                // Check if ad is paused (cannot delete active ads)
                if (ad.Status?.ToUpper() == "ACTIVE")
                {
                    throw new InvalidOperationException("Cannot delete active ad. Please pause the ad first.");
                }

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(ad.AdSet.Campaign.BrandId);
                if (socialIntegration != null && socialIntegration.IsActive && !string.IsNullOrEmpty(ad.AdId))
                {
                    // Delete from Facebook
                    await _facebookApiService.DeleteAdAsync(ad.AdId, socialIntegration.AccessToken);
                }

                // Soft delete from database
                var deleted = await _adRepository.SoftDeleteAsync(adId);
                if (deleted)
                {
                    await SendNotificationAsync(userId, "Ad Deleted", 
                        $"Your ad has been deleted successfully.", 
                        adId, "ad");

                    _logger.LogInformation("User {UserId} deleted ad {AdId}", userId, adId);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ad {AdId} for user {UserId}", adId, userId);
                throw;
            }
        }

        public async Task<AdPerformanceResponse?> PullReportsAsync(Guid userId, Guid adId)
        {
            try
            {
                var ad = await _adRepository.GetByIdWithDetailsAsync(adId);
                if (ad == null)
                {
                    throw new ArgumentException("Ad not found");
                }

                // Validate access
                await ValidateAdAccessAsync(userId, ad);

                if (string.IsNullOrEmpty(ad.AdId))
                {
                    throw new ArgumentException("Ad does not have a Facebook ID");
                }

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(ad.AdSet.Campaign.BrandId);
                if (socialIntegration == null || !socialIntegration.IsActive)
                {
                    throw new ArgumentException("No active social integration found");
                }

                // Check token validity
                var isTokenValid = await _facebookApiService.CheckTokenExpiryAsync(socialIntegration.AccessToken);
                if (!isTokenValid)
                {
                    throw new UnauthorizedAccessException("Facebook access token has expired. Please reconnect your account.");
                }

                // Get insights from Facebook
                var performanceData = await _facebookApiService.GetAdInsightsAsync(ad.AdId, socialIntegration.AccessToken);
                if (performanceData == null)
                {
                    throw new Exception("Failed to retrieve ad insights from Facebook");
                }

                // Save to database
                var performanceReport = new PerformanceReport
                {
                    AdId = adId,
                    Impressions = performanceData.Impressions,
                    Engagement = performanceData.Engagement,
                    Ctr = performanceData.Ctr,
                    EstimatedRevenue = performanceData.EstimatedRevenue,
                    ReportDate = performanceData.ReportDate,
                    RawData = performanceData.RawData
                };

                await _performanceReportRepository.CreateOrUpdateAsync(performanceReport);

                _logger.LogInformation("User {UserId} pulled reports for ad {AdId}", userId, adId);

                return performanceData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling reports for ad {AdId} and user {UserId}", adId, userId);
                throw;
            }
        }

        public async Task PullAllActiveAdsReportsAsync()
        {
            try
            {
                var activeAds = await _adRepository.GetActiveAdsAsync();
                var tasks = activeAds.Select(ad => PullReportsForAdAsync(ad));
                await Task.WhenAll(tasks);

                _logger.LogInformation("Pulled reports for {Count} active ads", activeAds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling reports for all active ads");
                throw;
            }
        }

        private async Task PullReportsForAdAsync(Ad ad)
        {
            try
            {
                if (string.IsNullOrEmpty(ad.AdId))
                {
                    return;
                }

                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(ad.AdSet.Campaign.BrandId);
                if (socialIntegration == null || !socialIntegration.IsActive)
                {
                    return;
                }

                var performanceData = await _facebookApiService.GetAdInsightsAsync(ad.AdId, socialIntegration.AccessToken);
                if (performanceData != null)
                {
                    var performanceReport = new PerformanceReport
                    {
                        AdId = ad.Id,
                        Impressions = performanceData.Impressions,
                        Engagement = performanceData.Engagement,
                        Ctr = performanceData.Ctr,
                        EstimatedRevenue = performanceData.EstimatedRevenue,
                        ReportDate = performanceData.ReportDate,
                        RawData = performanceData.RawData
                    };

                    await _performanceReportRepository.CreateOrUpdateAsync(performanceReport);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling reports for ad {AdId}", ad.Id);
            }
        }

        // Validation methods (simplified for brevity)
        private async Task ValidateAdSetAccessAsync(Guid userId, AdSet adSet)
        {
            await ValidateCampaignAccessAsync(userId, adSet.Campaign);
        }

        private async Task ValidateCreativeAccessAsync(Guid userId, AdCreative creative)
        {
            await ValidateContentAccessAsync(userId, creative.Content);
        }

        private async Task ValidateAdAccessAsync(Guid userId, Ad ad)
        {
            await ValidateCampaignAccessAsync(userId, ad.AdSet.Campaign);
        }

        private async Task ValidateCampaignAccessAsync(Guid userId, AdCampaign campaign)
        {
            if (campaign.Brand.UserId == userId)
            {
                return;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.Role == Data.Enumeration.UserRoleEnum.Admin)
            {
                return;
            }

            throw new UnauthorizedAccessException("You do not have permission to access this campaign");
        }

        private async Task ValidateContentAccessAsync(Guid userId, Content content)
        {
            if (content.Brand.UserId == userId)
            {
                return;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.Role == Data.Enumeration.UserRoleEnum.Admin)
            {
                return;
            }

            throw new UnauthorizedAccessException("You do not have permission to access this content");
        }

        private async Task ValidateBrandAccessAsync(Guid userId, Brand brand)
        {
            if (brand.UserId == userId)
            {
                return;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.Role == Data.Enumeration.UserRoleEnum.Admin)
            {
                return;
            }

            throw new UnauthorizedAccessException("You do not have permission to access this brand");
        }

        private async Task<Brand?> GetBrandByIdAsync(Guid brandId)
        {
            // This would typically use IBrandRepository
            // For now, return null to avoid circular dependency
            return null;
        }

        private async Task<PagedResult<Ad>> GetUserAdsAsync(Guid userId, int page, int pageSize)
        {
            // This would typically get all ads for user's brands
            // For now, return empty result to avoid circular dependency
            return new PagedResult<Ad>
            {
                Data = new List<Ad>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task SendNotificationAsync(Guid userId, string title, string message, Guid targetId, string targetType)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = Data.Enumeration.NotificationTypeEnum.SystemUpdate,
                    TargetId = targetId,
                    TargetType = targetType
                };

                await _notificationRepository.CreateAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            }
        }

        private static AdResponse MapToResponse(Ad ad)
        {
            return new AdResponse
            {
                Id = ad.Id,
                AdSetId = ad.AdSetId,
                CreativeId = ad.CreativeId,
                AdId = ad.AdId,
                Status = ad.Status,
                CreatedAt = ad.CreatedAt
            };
        }
    }
}
