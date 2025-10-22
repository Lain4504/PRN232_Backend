using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class AdCreativeService : IAdCreativeService
    {
        private readonly IAdCreativeRepository _adCreativeRepository;
        private readonly IContentRepository _contentRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IFacebookMarketingApiService _facebookApiService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<AdCreativeService> _logger;

        public AdCreativeService(
            IAdCreativeRepository adCreativeRepository,
            IContentRepository contentRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IFacebookMarketingApiService facebookApiService,
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            ITeamMemberRepository teamMemberRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<AdCreativeService> logger)
        {
            _adCreativeRepository = adCreativeRepository;
            _contentRepository = contentRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _facebookApiService = facebookApiService;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _teamMemberRepository = teamMemberRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _logger = logger;
        }

        public async Task<AdCreativeResponse> CreateAdCreativeAsync(Guid userId, CreateAdCreativeRequest request)
        {
            try
            {
                // Validate content exists and is approved
                var content = await _contentRepository.GetByIdAsync(request.ContentId);
                if (content == null)
                {
                    throw new ArgumentException("Content not found");
                }

                if (content.Status != ContentStatusEnum.Approved)
                {
                    throw new ArgumentException("Content must be approved before creating ad creative");
                }

                // Validate user has access to the content's brand
                await ValidateContentAccessAsync(userId, content);

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(content.BrandId);
                if (socialIntegration == null || !socialIntegration.IsActive || string.IsNullOrEmpty(socialIntegration.AdAccountId))
                {
                    throw new ArgumentException("No active social integration found for this brand");
                }

                if (socialIntegration.AdAccountId != request.AdAccountId)
                {
                    throw new ArgumentException("Ad account ID does not match the brand's social integration");
                }

                // Check token validity
                var isTokenValid = await _facebookApiService.CheckTokenExpiryAsync(socialIntegration.AccessToken);
                if (!isTokenValid)
                {
                    throw new UnauthorizedAccessException("Facebook access token has expired. Please reconnect your account.");
                }

                // Validate call to action
                var validCallToActions = new[] { "SHOP_NOW", "LEARN_MORE", "SIGN_UP", "DOWNLOAD", "BOOK_TRAVEL", "GET_QUOTE" };
                if (!string.IsNullOrEmpty(request.CallToAction) && !validCallToActions.Contains(request.CallToAction.ToUpper()))
                {
                    throw new ArgumentException($"Invalid call to action. Valid options: {string.Join(", ", validCallToActions)}");
                }

                // Prepare creative content
                var pageId = socialIntegration.ExternalId; // Facebook Page ID
                if (string.IsNullOrEmpty(pageId))
                {
                    throw new ArgumentException("Page ID not found in social integration");
                }

                var message = content.TextContent;
                var imageUrl = GetImageUrl(content);
                var videoUrl = content.VideoUrl;
                var callToAction = request.CallToAction?.ToUpper();

                // Create ad creative on Facebook
                var facebookCreativeId = await _facebookApiService.CreateAdCreativeAsync(
                    socialIntegration.AdAccountId,
                    pageId,
                    message,
                    imageUrl,
                    videoUrl,
                    callToAction,
                    socialIntegration.AccessToken);

                // Save to database
                var adCreative = new AdCreative
                {
                    ContentId = request.ContentId,
                    AdAccountId = request.AdAccountId,
                    CreativeId = facebookCreativeId,
                    CallToAction = callToAction
                };

                var createdCreative = await _adCreativeRepository.CreateAsync(adCreative);

                // Send notification
                await SendNotificationAsync(userId, "Ad Creative Created", 
                    $"Ad creative has been created successfully from content '{content.Title}'.", 
                    createdCreative.Id, "ad_creative");

                _logger.LogInformation("User {UserId} created ad creative {CreativeId} with Facebook ID {FacebookCreativeId}", 
                    userId, createdCreative.Id, facebookCreativeId);

                return MapToResponse(createdCreative, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad creative for user {UserId}", userId);
                throw;
            }
        }

        public async Task<AdCreativeResponse?> GetAdCreativeByIdAsync(Guid userId, Guid creativeId)
        {
            try
            {
                var creative = await _adCreativeRepository.GetByIdWithDetailsAsync(creativeId);
                if (creative == null) return null;

                // Validate access through content
                await ValidateContentAccessAsync(userId, creative.Content);

                return MapToResponse(creative, creative.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad creative {CreativeId} for user {UserId}", creativeId, userId);
                throw;
            }
        }

        public async Task<AdCreativeResponse?> GetAdCreativeByContentAsync(Guid userId, Guid contentId)
        {
            try
            {
                // Validate content access
                var content = await _contentRepository.GetByIdAsync(contentId);
                if (content == null)
                {
                    throw new ArgumentException("Content not found");
                }

                await ValidateContentAccessAsync(userId, content);

                var creative = await _adCreativeRepository.GetByContentIdAsync(contentId);
                if (creative == null) return null;

                return MapToResponse(creative, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad creative for content {ContentId} and user {UserId}", contentId, userId);
                throw;
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
                throw new UnauthorizedAccessException("You do not have permission to create ad creatives for this content");
            }
        }

        private string? GetImageUrl(Content content)
        {
            if (string.IsNullOrEmpty(content.ImageUrl))
            {
                return null;
            }

            // If ImageUrl is JSON, try to parse it
            try
            {
                var imageUrls = System.Text.Json.JsonSerializer.Deserialize<string[]>(content.ImageUrl);
                return imageUrls?.FirstOrDefault();
            }
            catch
            {
                // If not JSON, return as is
                return content.ImageUrl;
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

        private AdCreativeResponse MapToResponse(AdCreative creative, Content content)
        {
            return new AdCreativeResponse
            {
                Id = creative.Id,
                ContentId = creative.ContentId,
                AdAccountId = creative.AdAccountId,
                CreativeId = creative.CreativeId,
                CallToAction = creative.CallToAction,
                CreatedAt = creative.CreatedAt,
                ContentPreview = new AdCreativePreview
                {
                    Title = content.Title,
                    TextContent = content.TextContent,
                    ImageUrl = GetImageUrl(content),
                    VideoUrl = content.VideoUrl,
                    AdType = content.AdType.ToString()
                }
            };
        }

    }
}
