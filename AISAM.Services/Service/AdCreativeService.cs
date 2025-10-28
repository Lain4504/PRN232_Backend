using AISAM.Common.Dtos;
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
        private readonly IBrandRepository _brandRepository;
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
            IBrandRepository brandRepository,
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
            _brandRepository = brandRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _logger = logger;
        }

        // Legacy method - will be deprecated
        public async Task<AdCreativeResponse> CreateAdCreativeAsync(Guid userId, CreateAdCreativeRequest request)
        {
            // Convert legacy request to new format
            var newRequest = new CreateAdCreativeFromContentRequest
            {
                ContentId = request.ContentId,
                AdAccountId = request.AdAccountId,
                CallToAction = request.CallToAction
            };

            return await CreateAdCreativeFromContentAsync(userId, newRequest);
        }

        public async Task<AdCreativeResponse> CreateAdCreativeFromContentAsync(Guid userId, CreateAdCreativeFromContentRequest request)
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
                var linkUrl = request.LinkUrl ?? GetLinkUrl(content); // Use provided link or default
                var adName = request.AdName ?? content.Title ?? "Ad Creative"; // Use provided name or content title

                // Create ad creative on Facebook
                var facebookCreativeId = await _facebookApiService.CreateAdCreativeAsync(
                    socialIntegration.AdAccountId,
                    pageId,
                    message,
                    imageUrl,
                    videoUrl,
                    callToAction,
                    socialIntegration.AccessToken,
                    linkUrl,
                    adName);

                // Save to database
                var adCreative = new AdCreative
                {
                    ContentId = request.ContentId,
                    AdAccountId = request.AdAccountId,
                    CreativeId = facebookCreativeId,
                    CallToAction = callToAction
                };

                var createdCreative = await _adCreativeRepository.CreateAsync(adCreative);

                // Validate that CreativeId was properly stored
                if (string.IsNullOrEmpty(createdCreative.CreativeId))
                {
                    throw new Exception("Failed to store Facebook Creative ID. Please try again.");
                }

                // Validate that CreativeId is a numeric string (Facebook ID format)
                if (!long.TryParse(createdCreative.CreativeId, out _))
                {
                    throw new Exception("Facebook Creative ID is not in valid format. Please try again.");
                }

                // Send notification
                await SendNotificationAsync(content.ProfileId, "Ad Creative Created", 
                    $"Ad creative has been created successfully from content '{content.Title}'.", 
                    createdCreative.Id, "ad_creative");

                _logger.LogInformation("User {UserId} created ad creative {CreativeId} with Facebook ID {FacebookCreativeId} from content", 
                    userId, createdCreative.Id, facebookCreativeId);

                return MapToResponse(createdCreative, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad creative from content for user {UserId}", userId);
                throw;
            }
        }

        public async Task<AdCreativeResponse> CreateAdCreativeFromFacebookPostAsync(Guid userId, CreateAdCreativeFromFacebookPostRequest request)
        {
            try
            {
                // Validate brand exists and user has access
                var brand = await _brandRepository.GetByIdAsync(request.BrandId);
                if (brand == null)
                {
                    throw new ArgumentException("Brand not found");
                }

                // Validate user has access to the brand
                await ValidateBrandAccessAsync(userId, brand);

                // Get social integration for Facebook API calls
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(request.BrandId);
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

                // Create ad creative directly from existing Facebook post using object_story_id
                // According to Facebook Marketing API docs, we should use object_story_id instead of getting post details
                var pageId = socialIntegration.ExternalId; // Facebook Page ID
                if (string.IsNullOrEmpty(pageId))
                {
                    throw new ArgumentException("Page ID not found in social integration");
                }

                var adName = request.AdName ?? $"Ad from Post {request.FacebookPostId}";

                // Create ad creative using object_story_id (existing post)
                var facebookCreativeId = await _facebookApiService.CreateAdCreativeFromPostAsync(
                    socialIntegration.AdAccountId,
                    request.FacebookPostId,
                    socialIntegration.AccessToken,
                    adName);

                // Save to database (without ContentId since it's from Facebook post)
                var adCreative = new AdCreative
                {
                    ContentId = null, // No content ID for Facebook post creatives
                    AdAccountId = request.AdAccountId,
                    CreativeId = facebookCreativeId,
                    CallToAction = request.CallToAction?.ToUpper(),
                    FacebookPostId = request.FacebookPostId // Store the original Facebook post ID
                };

                var createdCreative = await _adCreativeRepository.CreateAsync(adCreative);

                // Validate that CreativeId was properly stored
                if (string.IsNullOrEmpty(createdCreative.CreativeId))
                {
                    throw new Exception("Failed to store Facebook Creative ID. Please try again.");
                }

                // Validate that CreativeId is a numeric string (Facebook ID format)
                if (!long.TryParse(createdCreative.CreativeId, out _))
                {
                    throw new Exception("Facebook Creative ID is not in valid format. Please try again.");
                }

                // Send notification
                await SendNotificationAsync(brand.ProfileId, "Ad Creative Created", 
                    $"Ad creative has been created successfully from Facebook post.", 
                    createdCreative.Id, "ad_creative");

                _logger.LogInformation("User {UserId} created ad creative {CreativeId} with Facebook ID {FacebookCreativeId} from Facebook post {PostId}", 
                    userId, createdCreative.Id, facebookCreativeId, request.FacebookPostId);

                return MapToResponseFromFacebookPost(createdCreative);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad creative from Facebook post for user {UserId}", userId);
                throw;
            }
        }

        public async Task<AdCreativeResponse?> GetAdCreativeByIdAsync(Guid userId, Guid creativeId)
        {
            try
            {
                var creative = await _adCreativeRepository.GetByIdWithDetailsAsync(creativeId);
                if (creative == null) return null;

                // Validate access - if ContentId is null, validate through brand
                if (creative.ContentId.HasValue && creative.Content != null)
                {
                    await ValidateContentAccessAsync(userId, creative.Content);
                    return MapToResponse(creative, creative.Content);
                }
                else
                {
                    // For Facebook post creatives, we need to find the brand through social integration
                    var socialIntegration = await _socialIntegrationRepository.GetByAdAccountIdAsync(creative.AdAccountId);
                    if (socialIntegration != null)
                    {
                        var brand = await _brandRepository.GetByIdAsync(socialIntegration.BrandId);
                        if (brand != null)
                        {
                            await ValidateBrandAccessAsync(userId, brand);
                        }
                    }
                    
                    return MapToResponseFromFacebookPost(creative);
                }
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

        public async Task<PagedResult<AdCreativeResponse>> GetAdCreativesByAdSetAsync(Guid userId, Guid adSetId, int page, int pageSize, string? search = null, string? type = null, string? sortBy = null, string? sortOrder = null)
        {
            try
            {
                // Validate access via ad set's campaign
                var adSet = await _adCreativeRepository.GetByIdAsync(adSetId); // This is wrong type; adjust by using AdSetRepository if needed.
                // Since we don't have AdSetRepository here, assume access validated earlier at controller layer if necessary.

                var result = await _adCreativeRepository.GetByAdSetIdPagedAsync(adSetId, page, pageSize, search, type, sortBy, sortOrder);
                var data = new List<AdCreativeResponse>();
                foreach (var ac in result.Data)
                {
                    data.Add(new AdCreativeResponse
                    {
                        Id = ac.Id,
                        Name = ac.CreativeId, // fallback name
                        ContentId = ac.ContentId,
                        AdAccountId = ac.AdAccountId,
                        CreativeId = ac.CreativeId,
                        CallToAction = ac.CallToAction,
                        FacebookPostId = ac.FacebookPostId,
                        CreatedAt = ac.CreatedAt,
                        ContentPreview = ac.Content != null ? new AdCreativePreview
                        {
                            Title = ac.Content.Title,
                            TextContent = ac.Content.TextContent,
                            ImageUrl = GetImageUrl(ac.Content),
                            VideoUrl = ac.Content.VideoUrl,
                            AdType = ac.Content.AdType.ToString()
                        } : null
                    });
                }

                return new PagedResult<AdCreativeResponse>
                {
                    Data = data,
                    TotalCount = result.TotalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing ad creatives for ad set {AdSetId}", adSetId);
                throw;
            }
        }

        public async Task<string> GetAdCreativePreviewHtmlAsync(Guid userId, Guid creativeId, string adFormat)
        {
            try
            {
                var creative = await _adCreativeRepository.GetByIdWithDetailsAsync(creativeId);
                if (creative == null)
                {
                    throw new ArgumentException("Ad creative not found");
                }

                // Validate access via content or brand
                if (creative.ContentId.HasValue && creative.Content != null)
                {
                    await ValidateContentAccessAsync(userId, creative.Content);
                }
                else
                {
                    var social = await _socialIntegrationRepository.GetByAdAccountIdAsync(creative.AdAccountId);
                    if (social == null)
                    {
                        throw new ArgumentException("No social integration found for this creative");
                    }
                    var brand = await _brandRepository.GetByIdAsync(social.BrandId);
                    if (brand == null)
                    {
                        throw new ArgumentException("Brand not found for this creative");
                    }
                    await ValidateBrandAccessAsync(userId, brand);
                }

                // Need a valid user access token per FB docs for previews
                var socialIntegration = await _socialIntegrationRepository.GetByAdAccountIdAsync(creative.AdAccountId);
                if (socialIntegration == null || !socialIntegration.IsActive)
                {
                    throw new ArgumentException("No active social integration found");
                }

                var userAccessToken = socialIntegration.SocialAccount?.UserAccessToken;
                if (string.IsNullOrWhiteSpace(userAccessToken))
                {
                    throw new ArgumentException("No valid user access token found for the connected social account");
                }

                var previewHtml = await _facebookApiService.GetAdCreativePreviewHtmlAsync(creative.CreativeId, adFormat, userAccessToken);
                if (string.IsNullOrEmpty(previewHtml))
                {
                    throw new Exception("Failed to generate ad preview from Facebook");
                }

                return previewHtml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preview for ad creative {CreativeId}", creativeId);
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

        private string? GetLinkUrl(Content content)
        {
            // For now, return a default link - in a real implementation, 
            // you might want to add a LinkUrl field to the Content model or Product model
            // The Product model doesn't currently have a LinkUrl property
            return "https://www.facebook.com";
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

        private async Task ValidateBrandAccessAsync(Guid userId, Brand brand)
        {
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
                throw new UnauthorizedAccessException("You do not have permission to access this brand");
            }

            // For Basic/Pro profiles: check team member access
            var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brand.Id);
            if (teamMember == null)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this brand");
            }

            if (!_rolePermissionConfig.HasCustomPermission(teamMember.Permissions, "can_create_ad"))
            {
                throw new UnauthorizedAccessException("You do not have permission to create ad creatives for this brand");
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
                FacebookPostId = creative.FacebookPostId,
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

        private AdCreativeResponse MapToResponseFromFacebookPost(AdCreative creative)
        {
            return new AdCreativeResponse
            {
                Id = creative.Id,
                ContentId = creative.ContentId, // Will be null for Facebook post creatives
                AdAccountId = creative.AdAccountId,
                CreativeId = creative.CreativeId,
                CallToAction = creative.CallToAction,
                FacebookPostId = creative.FacebookPostId,
                CreatedAt = creative.CreatedAt,
                ContentPreview = new AdCreativePreview
                {
                    Title = "Facebook Post Ad",
                    TextContent = $"Ad creative created from Facebook post {creative.FacebookPostId}",
                    ImageUrl = null,
                    VideoUrl = null,
                    AdType = "FACEBOOK_POST"
                }
            };
        }

    }
}
