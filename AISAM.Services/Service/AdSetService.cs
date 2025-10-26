using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
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
        private readonly IUserRepository _userRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<AdSetService> _logger;

        public AdSetService(
            IAdSetRepository adSetRepository,
            IAdCampaignRepository adCampaignRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IFacebookMarketingApiService facebookApiService,
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            ITeamMemberRepository teamMemberRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<AdSetService> logger)
        {
            _adSetRepository = adSetRepository;
            _adCampaignRepository = adCampaignRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _facebookApiService = facebookApiService;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _teamMemberRepository = teamMemberRepository;
            _rolePermissionConfig = rolePermissionConfig;
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

                // Validate that FacebookCampaignId is a numeric string (Facebook ID format)
                if (!long.TryParse(campaign.FacebookCampaignId, out _))
                {
                    throw new ArgumentException("Campaign Facebook ID is not in valid format. Please recreate the campaign.");
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
                    EndDate = request.EndDate,
                    Status = "PAUSED" // Default status for new ad sets
                };

                var createdAdSet = await _adSetRepository.CreateAsync(adSet);

                // Validate that FacebookAdSetId was properly stored
                if (string.IsNullOrEmpty(createdAdSet.FacebookAdSetId))
                {
                    throw new Exception("Failed to store Facebook Ad Set ID. Please try again.");
                }

                // Send notification
                await SendNotificationAsync(campaign.ProfileId, "Ad Set Created", 
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
                var campaign = await _adCampaignRepository.GetByIdWithDetailsAsync(campaignId);
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
            var brand = campaign.Brand;
            if (brand == null)
            {
                throw new ArgumentException("Campaign brand not found");
            }
            
            // Check if user directly owns the brand
            var profiles = await _profileRepository.GetByUserIdAsync(userId);
            if (profiles?.Any(p => p.Id == brand.ProfileId) == true)
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
            var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, campaign.BrandId);
            if (teamMember == null)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this campaign");
            }

            if (!_rolePermissionConfig.HasCustomPermission(teamMember.Permissions, "can_create_ad"))
            {
                throw new UnauthorizedAccessException("You do not have permission to access this campaign");
            }
        }

        private async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _userRepository.GetByIdAsync(userId);
        }

        private async Task<TeamMember?> GetTeamMemberByUserAndBrandAsync(Guid userId, Guid brandId)
        {
            return await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brandId);
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

        public async Task<bool> UpdateAdSetStatusAsync(Guid userId, UpdateAdSetStatusRequest request)
        {
            try
            {
                var adSet = await _adSetRepository.GetByIdWithDetailsAsync(request.AdSetId);
                if (adSet == null)
                {
                    throw new ArgumentException("Ad set not found");
                }

                // Validate access
                await ValidateCampaignAccessAsync(userId, adSet.Campaign);

                // Validate status
                var validStatuses = new[] { "ACTIVE", "PAUSED" };
                if (!validStatuses.Contains(request.Status.ToUpper()))
                {
                    throw new ArgumentException($"Invalid status. Valid options: {string.Join(", ", validStatuses)}");
                }

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(adSet.Campaign.BrandId);
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

                // Update status on Facebook if FacebookAdSetId exists
                if (!string.IsNullOrEmpty(adSet.FacebookAdSetId))
                {
                    var facebookSuccess = await _facebookApiService.UpdateAdSetStatusAsync(adSet.FacebookAdSetId, request.Status, socialIntegration.AccessToken);
                    if (!facebookSuccess)
                    {
                        throw new Exception("Failed to update ad set status on Facebook");
                    }
                }

                // Update status in database
                var updated = await _adSetRepository.UpdateStatusAsync(adSet.Id, request.Status);
                if (updated)
                {
                    // Send notification
                    var notificationTitle = request.Status.ToUpper() == "ACTIVE" ? "Ad Set Resumed" : "Ad Set Paused";
                    var notificationMessage = request.Status.ToUpper() == "ACTIVE" 
                        ? $"Your ad set '{adSet.Name}' has been resumed and is now running."
                        : $"Your ad set '{adSet.Name}' has been paused.";

                    await SendNotificationAsync(adSet.Campaign.ProfileId, notificationTitle, notificationMessage, adSet.Id, "ad_set");

                    _logger.LogInformation("User {UserId} updated ad set {AdSetId} status to {Status}", userId, adSet.Id, request.Status);
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ad set status for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteAdSetAsync(Guid userId, Guid adSetId)
        {
            try
            {
                var adSet = await _adSetRepository.GetByIdWithDetailsAsync(adSetId);
                if (adSet == null)
                {
                    return false;
                }

                // Validate access
                await ValidateCampaignAccessAsync(userId, adSet.Campaign);

                // Check if ad set has active ads
                if (adSet.Ads != null && adSet.Ads.Any(a => a.Status?.ToUpper() == "ACTIVE"))
                {
                    throw new InvalidOperationException("Cannot delete ad set with active ads. Please pause or delete the ads first.");
                }

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(adSet.Campaign.BrandId);
                if (socialIntegration != null && socialIntegration.IsActive && !string.IsNullOrEmpty(adSet.FacebookAdSetId))
                {
                    // Delete from Facebook
                    await _facebookApiService.DeleteAdSetAsync(adSet.FacebookAdSetId, socialIntegration.AccessToken);
                }

                // Soft delete from database
                var deleted = await _adSetRepository.SoftDeleteAsync(adSetId);
                if (deleted)
                {
                    await SendNotificationAsync(adSet.Campaign.ProfileId, "Ad Set Deleted", 
                        $"Your ad set '{adSet.Name}' has been deleted successfully.", 
                        adSetId, "ad_set");

                    _logger.LogInformation("User {UserId} deleted ad set {AdSetId}", userId, adSetId);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ad set {AdSetId} for user {UserId}", adSetId, userId);
                throw;
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
                Status = adSet.Status,
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
