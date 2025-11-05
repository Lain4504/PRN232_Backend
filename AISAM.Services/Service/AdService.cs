using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
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
        private readonly ISocialAccountRepository _socialAccountRepository;
        private readonly IPerformanceReportRepository _performanceReportRepository;
        private readonly IAdQuotaService _adQuotaService;
        private readonly IFacebookMarketingApiService _facebookApiService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly Dictionary<string, IProviderService> _providers;
        private readonly ILogger<AdService> _logger;

        public AdService(
            IAdRepository adRepository,
            IAdSetRepository adSetRepository,
            IAdCreativeRepository adCreativeRepository,
            IAdCampaignRepository adCampaignRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            ISocialAccountRepository socialAccountRepository,
            IPerformanceReportRepository performanceReportRepository,
            IAdQuotaService adQuotaService,
            IFacebookMarketingApiService facebookApiService,
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            ITeamMemberRepository teamMemberRepository,
            IBrandRepository brandRepository,
            RolePermissionConfig rolePermissionConfig,
            IEnumerable<IProviderService> providers,
            ILogger<AdService> logger)
        {
            _adRepository = adRepository;
            _adSetRepository = adSetRepository;
            _adCreativeRepository = adCreativeRepository;
            _adCampaignRepository = adCampaignRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _socialAccountRepository = socialAccountRepository;
            _performanceReportRepository = performanceReportRepository;
            _adQuotaService = adQuotaService;
            _facebookApiService = facebookApiService;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _teamMemberRepository = teamMemberRepository;
            _brandRepository = brandRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p);
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
                if (socialIntegration == null || !socialIntegration.IsActive)
                {
                    throw new ArgumentException("No active social integration found for this brand");
                }

                // Check token validity
                var isTokenValid = await _facebookApiService.CheckTokenExpiryAsync(socialIntegration.AccessToken);
                if (!isTokenValid)
                {
                    throw new UnauthorizedAccessException("Facebook access token has expired. Please reconnect your account.");
                }

                // Validate Facebook IDs exist
                if (string.IsNullOrEmpty(adSet.FacebookAdSetId))
                {
                    throw new ArgumentException("Ad set does not have a Facebook Ad Set ID. Please ensure the ad set was created successfully on Facebook.");
                }

                if (string.IsNullOrEmpty(creative.CreativeId))
                {
                    throw new ArgumentException("Ad creative does not have a Facebook Creative ID. Please ensure the creative was created successfully on Facebook.");
                }

                // Create ad on Facebook (AdAccountId comes from campaign)
                var facebookAdId = await _facebookApiService.CreateAdAsync(
                    adSet.Campaign.AdAccountId,
                    adSet.FacebookAdSetId, // Use Facebook Ad Set ID, not internal GUID
                    creative.CreativeId,
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

                await SendNotificationAsync(adSet.Campaign.ProfileId, notificationTitle, notificationMessage, createdAd.Id, "ad");

                _logger.LogInformation("User {UserId} created ad {AdId} with Facebook ID {FacebookAdId}", 
                    userId, createdAd.Id, facebookAdId);

                return await MapToResponseAsync(createdAd);
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

                var responses = new List<AdResponse>();
                foreach (var ad in ads.Data)
                {
                    responses.Add(await MapToResponseAsync(ad));
                }
                
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

                return await MapToResponseAsync(ad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad {AdId} for user {UserId}", adId, userId);
                throw;
            }
        }

        public async Task<string> GetAdPreviewHtmlAsync(Guid userId, Guid adId, string adFormat)
        {
            try
            {
                var ad = await _adRepository.GetByIdWithDetailsAsync(adId);
                if (ad == null)
                {
                    throw new ArgumentException("Ad not found");
                }

                await ValidateAdAccessAsync(userId, ad);

                if (string.IsNullOrEmpty(ad.AdId))
                {
                    throw new ArgumentException("Ad does not have a Facebook ID");
                }

                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(ad.AdSet.Campaign.BrandId);
                if (socialIntegration == null || !socialIntegration.IsActive)
                {
                    throw new ArgumentException("No active social integration found");
                }

                // Per Facebook docs, previews require a User access token, not a Page token
                var userAccessToken = socialIntegration.SocialAccount?.UserAccessToken;
                if (string.IsNullOrWhiteSpace(userAccessToken))
                {
                    throw new ArgumentException("No valid user access token found for the connected social account");
                }

                var html = await _facebookApiService.GetAdPreviewHtmlAsync(ad.AdId, adFormat, userAccessToken);
                if (string.IsNullOrEmpty(html))
                {
                    throw new Exception("Failed to generate ad preview from Facebook");
                }
                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ad preview for ad {AdId}", adId);
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

                    await SendNotificationAsync(ad.AdSet.Campaign.ProfileId, notificationTitle, notificationMessage, ad.Id, "ad");

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
                    await SendNotificationAsync(ad.AdSet.Campaign.ProfileId, "Ad Deleted", 
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
            // If creative is from content, validate through content
            if (creative.ContentId.HasValue && creative.Content != null)
            {
                await ValidateContentAccessAsync(userId, creative.Content);
            }
            else
            {
                // For Facebook post creatives, validate through campaign if ad exists
                // Otherwise, skip validation as it was already validated during creation
                if (creative.Ads != null && creative.Ads.Any())
                {
                    var firstAd = creative.Ads.First();
                    if (firstAd.AdSet?.Campaign != null)
                    {
                        await ValidateCampaignAccessAsync(userId, firstAd.AdSet.Campaign);
                    }
                }
                // Note: If creative has no ads, validation was already done during creation
            }
        }

        private async Task ValidateAdAccessAsync(Guid userId, Ad ad)
        {
            await ValidateCampaignAccessAsync(userId, ad.AdSet.Campaign);
        }

        private async Task ValidateCampaignAccessAsync(Guid userId, AdCampaign campaign)
        {
            if (campaign == null)
            {
                throw new UnauthorizedAccessException("Invalid campaign reference");
            }

            // Ensure brand is loaded
            var brand = campaign.Brand;
            if (brand == null)
            {
                brand = await _brandRepository.GetByIdAsync(campaign.BrandId);
                if (brand == null)
                {
                    throw new UnauthorizedAccessException("Brand not found for campaign");
                }
            }
            
            // Check if user directly owns the brand
            var profiles = await _profileRepository.GetByUserIdAsync(userId) 
                           ?? Enumerable.Empty<Profile>();
            if (profiles.Any(p => p != null && p.Id == brand.ProfileId))
            {
                return;
            }

            // If brand's profile is Free type, only owner can access
            var brandProfile = await _profileRepository.GetByIdAsync(brand.ProfileId);
            if (brandProfile?.ProfileType == Data.Enumeration.ProfileTypeEnum.Free)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this campaign");
            }

            // For Basic/Pro profiles: check team member access
            var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brand.Id);
            if (teamMember == null)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this campaign");
            }

            if (!_rolePermissionConfig.HasCustomPermission(teamMember.Permissions, "can_create_ad"))
            {
                throw new UnauthorizedAccessException("You do not have permission to access this campaign");
            }
        }

        private async Task ValidateContentAccessAsync(Guid userId, Content content)
        {
            var brand = content.Brand;
            
            // Check if user directly owns the brand
            var profiles = await _profileRepository.GetByUserIdAsync(userId);
            if (profiles.Any(p => p.Id == brand.ProfileId))
            {
                return;
            }

            // If brand's profile is Free type, only owner can access
            var brandProfile = await _profileRepository.GetByIdAsync(brand.ProfileId);
            if (brandProfile?.ProfileType == Data.Enumeration.ProfileTypeEnum.Free)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this content");
            }

            // For Basic/Pro profiles: check team member access
            var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, content.BrandId);
            if (teamMember == null)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this content");
            }

            if (!_rolePermissionConfig.HasCustomPermission(teamMember.Permissions, "can_create_ad"))
            {
                throw new UnauthorizedAccessException("You do not have permission to access this content");
            }
        }

        private async Task ValidateBrandAccessAsync(Guid userId, Brand brand)
        {
            // Check if user directly owns the brand (through any of their profiles)
            var profiles = await _profileRepository.GetByUserIdAsync(userId);
            if (profiles.Any(p => p.Id == brand.ProfileId))
            {
                return;
            }

            // If brand's profile is Free type, only owner can access
            var brandProfile = await _profileRepository.GetByIdAsync(brand.ProfileId);
            if (brandProfile?.ProfileType == Data.Enumeration.ProfileTypeEnum.Free)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this brand");
            }

            // For Basic/Pro profiles: check team member access with permission
            var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brand.Id);
            if (teamMember == null)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this brand");
            }

            if (!_rolePermissionConfig.HasCustomPermission(teamMember.Permissions, "can_create_ad"))
            {
                throw new UnauthorizedAccessException("You do not have permission to access this brand");
            }
        }

        private async Task<Brand?> GetBrandByIdAsync(Guid brandId)
        {
            return await _brandRepository.GetByIdAsync(brandId);
        }

        private async Task<PagedResult<Ad>> GetUserAdsAsync(Guid userId, int page, int pageSize)
        {
            // Get user's brands and team brands with permissions
            var profiles = await _profileRepository.GetByUserIdAsync(userId);
            var ownedBrandIds = new List<Guid>();
            
            foreach (var profile in profiles)
            {
                var brands = await _brandRepository.GetPagedByProfileIdAsync(profile.Id, new PaginationRequest { Page = 1, PageSize = 10000 });
                ownedBrandIds.AddRange(brands.Data.Select(b => b.Id));
            }
            
            // Add team brands with permission
            var teamMembers = await _teamMemberRepository.GetByUserIdWithBrandsAsync(userId);
            var teamBrandIds = teamMembers
                .Where(tm => _rolePermissionConfig.HasCustomPermission(tm.Permissions, "can_create_ad"))
                .SelectMany(tm => tm.Team.TeamBrands.Select(tb => tb.BrandId))
                .Distinct()
                .ToList();
            
            var allBrandIds = ownedBrandIds.Union(teamBrandIds).Distinct().ToList();
            
            // Get ads for all accessible brands
            var allAds = new List<Ad>();
            foreach (var brandId in allBrandIds)
            {
                var ads = await _adRepository.GetByBrandIdAsync(brandId, 1, 10000);
                allAds.AddRange(ads.Data);
            }
            
            var pagedAds = allAds
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return new PagedResult<Ad>
            {
                Data = pagedAds,
                TotalCount = allAds.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task SendNotificationAsync(Guid profileId, string title, string message, Guid targetId, string targetType)
        {
            try
            {
                var notification = new Notification
                {
                    ProfileId = profileId,
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
                _logger.LogError(ex, "Failed to send notification to profile {ProfileId}", profileId);
            }
        }

        private async Task<AdResponse> MapToResponseAsync(Ad ad)
        {
            string? pageName = null;
            
            // Try to get page name from brand's social integration
            var brandId = ad.AdSet?.Campaign?.BrandId;
            if (brandId.HasValue && brandId.Value != Guid.Empty)
            {
                try
                {
                    var integration = await _socialIntegrationRepository.GetByBrandIdAsync(brandId.Value);
                    if (integration != null && integration.Platform == SocialPlatformEnum.Facebook && !string.IsNullOrEmpty(integration.ExternalId))
                    {
                        // Try to get page name from Facebook API
                        if (_providers.TryGetValue("facebook", out var providerService) && integration.SocialAccountId != Guid.Empty)
                        {
                            try
                            {
                                var account = await _socialAccountRepository.GetByIdAsync(integration.SocialAccountId);
                                if (account != null && !string.IsNullOrEmpty(account.UserAccessToken))
                                {
                                    var availableTargets = (await providerService.GetTargetsAsync(account.UserAccessToken)).ToList();
                                    var target = availableTargets.FirstOrDefault(t => t.ProviderTargetId == integration.ExternalId);
                                    if (target != null && !string.IsNullOrEmpty(target.Name))
                                    {
                                        pageName = target.Name;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to fetch Facebook page name for ad {AdId}, using fallback", ad.Id);
                            }
                        }
                        
                        // Don't use page ID as fallback - let frontend use other fields
                        // Only set pageName if we actually have a name
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get page name for ad {AdId}", ad.Id);
                }
            }
            
            return new AdResponse
            {
                Id = ad.Id,
                AdSetId = ad.AdSetId,
                CreativeId = ad.CreativeId,
                AdId = ad.AdId,
                Status = ad.Status,
                Name = !string.IsNullOrEmpty(ad.AdId) ? ad.AdId : null,
                AdSetName = ad.AdSet?.Name,
                PageName = pageName,
                CreatedAt = ad.CreatedAt
            };
        }
    }
}
