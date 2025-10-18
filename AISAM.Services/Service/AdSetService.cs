using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class AdSetService : IAdSetService
    {
        private readonly IAdSetRepository _adSetRepository;
        private readonly IAdCampaignRepository _adCampaignRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IFacebookMarketingApiService _facebookApiService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<AdSetService> _logger;

        public AdSetService(
            IAdSetRepository adSetRepository,
            IAdCampaignRepository adCampaignRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IFacebookMarketingApiService facebookApiService,
            INotificationRepository notificationRepository,
            ILogger<AdSetService> logger)
        {
            _adSetRepository = adSetRepository;
            _adCampaignRepository = adCampaignRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _facebookApiService = facebookApiService;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<AdSetResponse> CreateAdSetAsync(Guid userId, CreateAdSetRequest request)
        {
            try
            {
                // Validate campaign exists and user has access
                var campaign = await _adCampaignRepository.GetByIdWithDetailsAsync(request.CampaignId);
                if (campaign == null)
                {
                    throw new ArgumentException("Campaign not found");
                }

                // Validate user has access to the campaign's brand
                await ValidateCampaignAccessAsync(userId, campaign);

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(campaign.BrandId);
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

                // Validate targeting JSON
                try
                {
                    System.Text.Json.JsonDocument.Parse(request.Targeting);
                }
                catch (System.Text.Json.JsonException)
                {
                    throw new ArgumentException("Invalid targeting JSON format");
                }

                // Validate Facebook Campaign ID exists
                if (string.IsNullOrEmpty(campaign.FacebookCampaignId))
                {
                    throw new ArgumentException("Campaign does not have a Facebook Campaign ID. Please ensure the campaign was created successfully on Facebook.");
                }

                // Create ad set on Facebook
                var facebookAdSetId = await _facebookApiService.CreateAdSetAsync(
                    socialIntegration.AdAccountId,
                    campaign.FacebookCampaignId, // Use actual Facebook Campaign ID
                    request.Name,
                    request.Targeting,
                    request.DailyBudget,
                    socialIntegration.AccessToken,
                    request.StartDate,
                    request.EndDate,
                    "REACH", // Default optimization goal
                    "IMPRESSIONS"); // Default billing event

                // Save to database
                var adSet = new AdSet
                {
                    CampaignId = request.CampaignId,
                    FacebookAdSetId = facebookAdSetId, // Store Facebook Ad Set ID
                    Name = request.Name,
                    Targeting = request.Targeting,
                    DailyBudget = request.DailyBudget,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate
                };

                var createdAdSet = await _adSetRepository.CreateAsync(adSet);

                // Send notification
                await SendNotificationAsync(userId, "Ad Set Created", 
                    $"Ad set '{request.Name}' has been created successfully for campaign '{campaign.Name}'.", 
                    createdAdSet.Id, "ad_set");

                _logger.LogInformation("User {UserId} created ad set {AdSetId} with Facebook ID {FacebookAdSetId}", 
                    userId, createdAdSet.Id, facebookAdSetId);

                return MapToResponse(createdAdSet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad set for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<AdSetResponse>> GetAdSetsByCampaignAsync(Guid userId, Guid campaignId)
        {
            try
            {
                // Validate campaign access
                var campaign = await _adCampaignRepository.GetByIdAsync(campaignId);
                if (campaign == null)
                {
                    throw new ArgumentException("Campaign not found");
                }

                await ValidateCampaignAccessAsync(userId, campaign);

                var adSets = await _adSetRepository.GetByCampaignIdAsync(campaignId);
                return adSets.Select(MapToResponse).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad sets for campaign {CampaignId} and user {UserId}", campaignId, userId);
                throw;
            }
        }

        public async Task<AdSetResponse?> GetAdSetByIdAsync(Guid userId, Guid adSetId)
        {
            try
            {
                var adSet = await _adSetRepository.GetByIdWithDetailsAsync(adSetId);
                if (adSet == null) return null;

                // Validate access through campaign
                await ValidateCampaignAccessAsync(userId, adSet.Campaign);

                return MapToResponse(adSet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad set {AdSetId} for user {UserId}", adSetId, userId);
                throw;
            }
        }

        private async Task ValidateCampaignAccessAsync(Guid userId, AdCampaign campaign)
        {
            // Check if user owns the brand
            if (campaign.Brand.ProfileId == userId)
            {
                return;
            }

            // Check if user is admin
            var user = await GetUserByIdAsync(userId);
            if (user?.Role == Data.Enumeration.UserRoleEnum.Admin)
            {
                return;
            }

            // Check if user is vendor with team access
            if (user?.Role == Data.Enumeration.UserRoleEnum.Vendor)
            {
                // This would typically check team membership and permissions
                // For now, we'll implement a basic check
                var teamMember = await GetTeamMemberByUserAndBrandAsync(userId, campaign.BrandId);
                if (teamMember != null && teamMember.Permissions.Contains("can_create_ad"))
                {
                    return;
                }
            }

            throw new UnauthorizedAccessException("You do not have permission to access this campaign");
        }

        private async Task<User?> GetUserByIdAsync(Guid userId)
        {
            // This would typically use IUserRepository
            // For now, return null to avoid circular dependency
            return null;
        }

        private async Task<TeamMember?> GetTeamMemberByUserAndBrandAsync(Guid userId, Guid brandId)
        {
            // This would typically use ITeamMemberRepository
            // For now, return null to avoid circular dependency
            return null;
        }

        private async Task SendNotificationAsync(Guid userId, string title, string message, Guid targetId, string targetType)
        {
            try
            {
                var notification = new Notification
                {
                    ProfileId = userId,
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

        private static AdSetResponse MapToResponse(AdSet adSet)
        {
            return new AdSetResponse
            {
                Id = adSet.Id,
                CampaignId = adSet.CampaignId,
                FacebookAdSetId = adSet.FacebookAdSetId,
                Name = adSet.Name,
                Targeting = adSet.Targeting,
                DailyBudget = adSet.DailyBudget,
                StartDate = adSet.StartDate,
                EndDate = adSet.EndDate,
                CreatedAt = adSet.CreatedAt,
                Ads = adSet.Ads?.Select(a => new AdResponse
                {
                    Id = a.Id,
                    AdSetId = a.AdSetId,
                    CreativeId = a.CreativeId,
                    AdId = a.AdId,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt
                }).ToList() ?? new List<AdResponse>()
            };
        }
    }
}
