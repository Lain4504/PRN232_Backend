using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class AdCampaignService : IAdCampaignService
    {
        private readonly IAdCampaignRepository _adCampaignRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IAdQuotaService _adQuotaService;
        private readonly IFacebookMarketingApiService _facebookApiService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdCampaignService> _logger;

        public AdCampaignService(
            IAdCampaignRepository adCampaignRepository,
            IBrandRepository brandRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IAdQuotaService adQuotaService,
            IFacebookMarketingApiService facebookApiService,
            INotificationRepository notificationRepository,
            ITeamMemberRepository teamMemberRepository,
            IUserRepository userRepository,
            ILogger<AdCampaignService> logger)
        {
            _adCampaignRepository = adCampaignRepository;
            _brandRepository = brandRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _adQuotaService = adQuotaService;
            _facebookApiService = facebookApiService;
            _notificationRepository = notificationRepository;
            _teamMemberRepository = teamMemberRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<AdCampaignResponse> CreateCampaignAsync(Guid userId, CreateAdCampaignRequest request)
        {
            try
            {
                // Validate brand ownership or team permissions
                await ValidateBrandAccessAsync(userId, request.BrandId);

                // Check quota
                // var quotaValidation = await _adQuotaService.ValidateQuotaAsync(userId, request.Budget);
                // if (!quotaValidation.canCreate)
                // {
                //     throw new UnauthorizedAccessException(quotaValidation.errorMessage ?? "Quota exceeded");
                // }

                // Get social integration with ad account
                var socialIntegration = await GetSocialIntegrationWithAdAccountAsync(request.BrandId, request.AdAccountId);
                
                // Check token validity
                var isTokenValid = await _facebookApiService.CheckTokenExpiryAsync(socialIntegration.AccessToken);
                if (!isTokenValid)
                {
                    throw new UnauthorizedAccessException("Facebook access token has expired. Please reconnect your account.");
                }

                // Clean ad account ID for storage (remove act_ prefix)
                var cleanAdAccountId = request.AdAccountId.Replace("act_", "");

                // Create campaign on Facebook
                var facebookCampaignId = await _facebookApiService.CreateCampaignAsync(
                    cleanAdAccountId, 
                    request.Name, 
                    request.Objective, 
                    request.Budget, 
                    socialIntegration.AccessToken);

                // Save to database
                var adCampaign = new AdCampaign
                {
                    UserId = userId,
                    BrandId = request.BrandId,
                    AdAccountId = cleanAdAccountId,
                    FacebookCampaignId = facebookCampaignId, // Store Facebook Campaign ID
                    Name = request.Name,
                    Objective = request.Objective,
                    Budget = request.Budget,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsActive = true
                };

                var createdCampaign = await _adCampaignRepository.CreateAsync(adCampaign);

                // Send notification
                await SendNotificationAsync(userId, "Ad Campaign Created", 
                    $"Your campaign '{request.Name}' has been created successfully.", 
                    createdCampaign.Id, "ad_campaign");

                _logger.LogInformation("User {UserId} created ad campaign {CampaignId} with Facebook ID {FacebookCampaignId}", 
                    userId, createdCampaign.Id, facebookCampaignId);

                return MapToResponse(createdCampaign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad campaign for user {UserId}", userId);
                throw;
            }
        }

        public async Task<PagedResult<AdCampaignResponse>> GetCampaignsAsync(Guid userId, Guid? brandId, int page = 1, int pageSize = 20)
        {
            try
            {
                PagedResult<AdCampaign> campaigns;

                if (brandId.HasValue)
                {
                    // Validate access to specific brand
                    await ValidateBrandAccessAsync(userId, brandId.Value);
                    campaigns = await _adCampaignRepository.GetByBrandIdAsync(brandId.Value, page, pageSize);
                }
                else
                {
                    // Get all campaigns for user (including team brands if vendor)
                    campaigns = await _adCampaignRepository.GetByUserIdAsync(userId, page, pageSize);
                    
                    // If user is vendor, also get campaigns from team brands
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user?.Role == Data.Enumeration.UserRoleEnum.Vendor)
                    {
                        var teamCampaigns = await GetTeamCampaignsAsync(userId, page, pageSize);
                        campaigns.Data.AddRange(teamCampaigns.Data);
                        campaigns.TotalCount += teamCampaigns.TotalCount;
                    }
                }

                var responses = campaigns.Data.Select(MapToResponse).ToList();
                
                return new PagedResult<AdCampaignResponse>
                {
                    Data = responses,
                    TotalCount = campaigns.TotalCount,
                    Page = campaigns.Page,
                    PageSize = campaigns.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting campaigns for user {UserId}", userId);
                throw;
            }
        }

        public async Task<AdCampaignResponse?> GetCampaignByIdAsync(Guid userId, Guid campaignId)
        {
            try
            {
                var campaign = await _adCampaignRepository.GetByIdWithDetailsAsync(campaignId);
                if (campaign == null) return null;

                // Validate access
                await ValidateBrandAccessAsync(userId, campaign.BrandId);

                return MapToResponse(campaign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting campaign {CampaignId} for user {UserId}", campaignId, userId);
                throw;
            }
        }

        public async Task<bool> DeleteCampaignAsync(Guid userId, Guid campaignId)
        {
            try
            {
                var campaign = await _adCampaignRepository.GetByIdAsync(campaignId);
                if (campaign == null) return false;

                // Validate access
                await ValidateBrandAccessAsync(userId, campaign.BrandId);

                // Check if campaign has active ads
                var hasActiveAds = await HasActiveAdsAsync(campaignId);
                if (hasActiveAds)
                {
                    throw new InvalidOperationException("Cannot delete campaign with active ads. Please pause or delete ads first.");
                }

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(campaign.BrandId);
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

                // Delete campaign from Facebook first
                var facebookDeleted = false;
                if (!string.IsNullOrEmpty(campaign.AdAccountId) && !string.IsNullOrEmpty(campaign.FacebookCampaignId))
                {
                    try
                    {
                        facebookDeleted = await _facebookApiService.DeleteCampaignAsync(
                            campaign.AdAccountId,
                            campaign.FacebookCampaignId, // Use actual Facebook Campaign ID
                            socialIntegration.AccessToken,
                            "DELETE_ANY"
                        );

                        if (!facebookDeleted)
                        {
                            _logger.LogWarning("Failed to delete campaign {CampaignId} from Facebook, but continuing with database deletion", campaignId);
                        }
                        else
                        {
                            _logger.LogInformation("Successfully deleted campaign {CampaignId} from Facebook", campaignId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting campaign {CampaignId} from Facebook, but continuing with database deletion", campaignId);
                        // Continue with database deletion even if Facebook deletion fails
                    }
                }

                // Delete from database
                var deleted = await _adCampaignRepository.SoftDeleteAsync(campaignId);
                if (deleted)
                {
                    var message = facebookDeleted 
                        ? $"Campaign '{campaign.Name}' has been deleted from Facebook and database."
                        : $"Campaign '{campaign.Name}' has been deleted from database. Facebook deletion may have failed.";

                    await SendNotificationAsync(userId, "Ad Campaign Deleted", message, campaignId, "ad_campaign");

                    _logger.LogInformation("User {UserId} deleted ad campaign {CampaignId} - Facebook: {FacebookDeleted}, Database: {DatabaseDeleted}", 
                        userId, campaignId, facebookDeleted, deleted);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting campaign {CampaignId} for user {UserId}", campaignId, userId);
                throw;
            }
        }

        private async Task ValidateBrandAccessAsync(Guid userId, Guid brandId)
        {
            var brand = await _brandRepository.GetByIdAsync(brandId);
            if (brand == null)
            {
                throw new ArgumentException("Brand not found");
            }

            // Check if user owns the brand
            if (brand.UserId == userId)
            {
                return;
            }

            // Check if user is vendor with team access
            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.Role == Data.Enumeration.UserRoleEnum.Vendor)
            {
                var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brandId);
                if (teamMember != null && teamMember.Permissions.Contains("can_create_ad"))
                {
                    return;
                }
            }

            // Check if user is admin
            if (user?.Role == Data.Enumeration.UserRoleEnum.Admin)
            {
                return;
            }

            throw new UnauthorizedAccessException("You do not have permission to access this brand");
        }

        private async Task<SocialIntegration> GetSocialIntegrationWithAdAccountAsync(Guid brandId, string adAccountId)
        {
            var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(brandId);
            if (socialIntegration == null || !socialIntegration.IsActive)
            {
                throw new ArgumentException("No active social integration found for this brand");
            }

            if (string.IsNullOrEmpty(socialIntegration.AdAccountId) || socialIntegration.AdAccountId != adAccountId)
            {
                throw new ArgumentException("Ad account ID does not match the brand's social integration");
            }

            return socialIntegration;
        }

        private async Task<PagedResult<AdCampaign>> GetTeamCampaignsAsync(Guid userId, int page, int pageSize)
        {
            // Get team brands where user has ad permissions
            var teamMembers = await _teamMemberRepository.GetByUserIdWithBrandsAsync(userId);
            var brandIds = teamMembers
                .Where(tm => tm.Permissions.Contains("can_create_ad") && tm.Team.TeamBrands.Any())
                .SelectMany(tm => tm.Team.TeamBrands.Select(tb => tb.BrandId))
                .Distinct()
                .ToList();

            if (!brandIds.Any())
            {
                return new PagedResult<AdCampaign> { Data = new List<AdCampaign>(), TotalCount = 0, Page = page, PageSize = pageSize };
            }

            // Get campaigns for team brands
            var allCampaigns = new List<AdCampaign>();
            foreach (var brandId in brandIds)
            {
                var campaigns = await _adCampaignRepository.GetByBrandIdAsync(brandId, 1, 1000); // Get all campaigns
                allCampaigns.AddRange(campaigns.Data);
            }

            var pagedCampaigns = allCampaigns
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<AdCampaign>
            {
                Data = pagedCampaigns,
                TotalCount = allCampaigns.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task<bool> HasActiveAdsAsync(Guid campaignId)
        {
            // This would typically check if campaign has any active ads
            // For now, return false as we'll implement this when we have the Ad repository
            return false;
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

        private static AdCampaignResponse MapToResponse(AdCampaign campaign)
        {
            return new AdCampaignResponse
            {
                Id = campaign.Id,
                UserId = campaign.UserId,
                BrandId = campaign.BrandId,
                AdAccountId = campaign.AdAccountId,
                FacebookCampaignId = campaign.FacebookCampaignId,
                Name = campaign.Name,
                Objective = campaign.Objective,
                Budget = campaign.Budget,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                IsActive = campaign.IsActive,
                CreatedAt = campaign.CreatedAt,
                UpdatedAt = campaign.UpdatedAt,
                AdSets = campaign.AdSets?.Select(ads => new AdSetResponse
                {
                    Id = ads.Id,
                    CampaignId = ads.CampaignId,
                    Name = ads.Name,
                    Targeting = ads.Targeting,
                    DailyBudget = ads.DailyBudget,
                    StartDate = ads.StartDate,
                    EndDate = ads.EndDate,
                    CreatedAt = ads.CreatedAt
                }).ToList() ?? new List<AdSetResponse>()
            };
        }
    }
}
