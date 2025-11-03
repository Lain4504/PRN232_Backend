using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using AISAM.API.Utils;
using AISAM.Repositories.IRepositories;
using AISAM.Data.Enumeration;
using Microsoft.Extensions.Logging;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/ad-creatives")]
    [Authorize]
    public class AdCreativesController : ControllerBase
    {
        private readonly IAdCreativeService _adCreativeService;
        private readonly IFacebookMarketingApiService _facebookApiService;
        private readonly ISocialService _socialService;
        private readonly IBrandRepository _brandRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly ILogger<AdCreativesController> _logger;

        public AdCreativesController(
            IAdCreativeService adCreativeService,
            IFacebookMarketingApiService facebookApiService,
            ISocialService socialService,
            IBrandRepository brandRepository,
            IProfileRepository profileRepository,
            ITeamMemberRepository teamMemberRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            ILogger<AdCreativesController> logger)
        {
            _adCreativeService = adCreativeService;
            _facebookApiService = facebookApiService;
            _socialService = socialService;
            _brandRepository = brandRepository;
            _profileRepository = profileRepository;
            _teamMemberRepository = teamMemberRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _logger = logger;
        }

        [HttpPost]
        [Obsolete("This endpoint is deprecated. Use /from-content or /from-facebook-post instead.")]
        public async Task<ActionResult<GenericResponse<AdCreativeResponse>>> CreateAdCreative([FromBody] CreateAdCreativeRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCreativeService.CreateAdCreativeAsync(userId, request);
                return Ok(GenericResponse<AdCreativeResponse>.CreateSuccess(result, "Ad creative created successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdCreativeResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad creative");
                return StatusCode(500, GenericResponse<AdCreativeResponse>.CreateError("Internal server error"));
            }
        }

        [HttpPost("from-content")]
        public async Task<ActionResult<GenericResponse<AdCreativeResponse>>> CreateAdCreativeFromContent([FromBody] CreateAdCreativeFromContentRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCreativeService.CreateAdCreativeFromContentAsync(userId, request);
                return Ok(GenericResponse<AdCreativeResponse>.CreateSuccess(result, "Ad creative created successfully from content"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdCreativeResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad creative from content");
                return StatusCode(500, GenericResponse<AdCreativeResponse>.CreateError("Internal server error"));
            }
        }

        [HttpPost("from-facebook-post")]
        public async Task<ActionResult<GenericResponse<AdCreativeResponse>>> CreateAdCreativeFromFacebookPost([FromBody] CreateAdCreativeFromFacebookPostRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCreativeService.CreateAdCreativeFromFacebookPostAsync(userId, request);
                return Ok(GenericResponse<AdCreativeResponse>.CreateSuccess(result, "Ad creative created successfully from Facebook post"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdCreativeResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad creative from Facebook post");
                return StatusCode(500, GenericResponse<AdCreativeResponse>.CreateError("Internal server error"));
            }
        }

        [HttpGet("{creativeId}")]
        public async Task<ActionResult<GenericResponse<AdCreativeResponse>>> GetAdCreativeById(Guid creativeId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCreativeService.GetAdCreativeByIdAsync(userId, creativeId);
                
                if (result == null)
                {
                    return NotFound(GenericResponse<AdCreativeResponse>.CreateError("Ad creative not found"));
                }

                return Ok(GenericResponse<AdCreativeResponse>.CreateSuccess(result, "Ad creative retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdCreativeResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad creative {CreativeId}", creativeId);
                return StatusCode(500, GenericResponse<AdCreativeResponse>.CreateError("Internal server error"));
            }
        }

        [HttpGet("content/{contentId}")]
        public async Task<ActionResult<GenericResponse<AdCreativeResponse>>> GetAdCreativeByContent(Guid contentId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCreativeService.GetAdCreativeByContentAsync(userId, contentId);
                
                if (result == null)
                {
                    return NotFound(GenericResponse<AdCreativeResponse>.CreateError("Ad creative not found for this content"));
                }

                return Ok(GenericResponse<AdCreativeResponse>.CreateSuccess(result, "Ad creative retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdCreativeResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad creative for content {ContentId}", contentId);
                return StatusCode(500, GenericResponse<AdCreativeResponse>.CreateError("Internal server error"));
            }
        }

        [HttpGet]
        public async Task<ActionResult<GenericResponse<AISAM.Common.Dtos.PagedResult<AdCreativeResponse>>>> ListAdCreatives(
            [FromQuery] Guid adSetId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? type = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCreativeService.GetAdCreativesByAdSetAsync(userId, adSetId, page, pageSize, search, type, sortBy, sortOrder);
                return Ok(GenericResponse<AISAM.Common.Dtos.PagedResult<AdCreativeResponse>>.CreateSuccess(result, "Ad creatives retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AISAM.Common.Dtos.PagedResult<AdCreativeResponse>>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing ad creatives for ad set {AdSetId}", adSetId);
                return StatusCode(500, GenericResponse<AISAM.Common.Dtos.PagedResult<AdCreativeResponse>>.CreateError("Internal server error"));
            }
        }

        [HttpGet("{creativeId}/previews")]
        public async Task<ActionResult<GenericResponse<string>>> GetAdCreativePreview(Guid creativeId, [FromQuery] string adFormat = "DESKTOP_FEED_STANDARD")
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var html = await _adCreativeService.GetAdCreativePreviewHtmlAsync(userId, creativeId, adFormat);
                return Ok(GenericResponse<string>.CreateSuccess(html, "Ad preview generated"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview for ad creative {CreativeId}", creativeId);
                return StatusCode(500, GenericResponse<string>.CreateError("Internal server error"));
            }
        }

        [HttpGet("facebook/posts")]
        public async Task<ActionResult<GenericResponse<List<FacebookPostDetails>>>> GetFacebookPosts(
            [FromQuery] Guid brandId,
            [FromQuery] string? pageId = null,
            [FromQuery] int limit = 50)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                // Get brand to verify access
                var brand = await _brandRepository.GetByIdAsync(brandId);
                if (brand == null)
                {
                    return NotFound(GenericResponse<List<FacebookPostDetails>>.CreateError("Brand not found"));
                }

                // Verify user has access to the brand (owner or team member)
                // Note: Team members may not have any profile, so we check owner first, then team member
                var userProfiles = await _profileRepository.GetByUserIdAsync(userId);
                var isOwner = userProfiles != null && userProfiles.Any(p => p.Id == brand.ProfileId);

                if (!isOwner)
                {
                    // User is not owner - check if they are a team member
                    // This handles the case where team member has no profile
                    var brandProfile = await _profileRepository.GetByIdAsync(brand.ProfileId);
                    if (brandProfile == null)
                    {
                        return NotFound(GenericResponse<List<FacebookPostDetails>>.CreateError("Brand profile not found"));
                    }

                    // For Free profiles, only owner can access
                    if (brandProfile.ProfileType == ProfileTypeEnum.Free)
                    {
                        return Forbid("Only brand owner can access this resource");
                    }

                    // For Basic/Pro profiles: check if user is a team member
                    var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brandId);
                    if (teamMember == null || !teamMember.IsActive)
                    {
                        return Forbid("You don't have access to this brand");
                    }
                }

                // Get social integration directly from brand (no need for profileId from context)
                var socialIntegration = await _socialIntegrationRepository.GetByBrandIdAsync(brandId);

                if (socialIntegration == null || !socialIntegration.IsActive || socialIntegration.Platform != SocialPlatformEnum.Facebook)
                {
                    return BadRequest(GenericResponse<List<FacebookPostDetails>>.CreateError("No active Facebook integration found for this brand"));
                }

                // Use pageId from query or from integration
                var finalPageId = pageId ?? socialIntegration.ExternalId;
                if (string.IsNullOrEmpty(finalPageId))
                {
                    return BadRequest(GenericResponse<List<FacebookPostDetails>>.CreateError("Page ID is required"));
                }

                // Get access token from integration
                var accessToken = socialIntegration.AccessToken;
                if (string.IsNullOrEmpty(accessToken))
                {
                    return BadRequest(GenericResponse<List<FacebookPostDetails>>.CreateError("Access token not found"));
                }

                // Get posts from Facebook API
                var posts = await _facebookApiService.GetFacebookPostsAsync(finalPageId, accessToken, limit);
                return Ok(GenericResponse<List<FacebookPostDetails>>.CreateSuccess(posts, "Facebook posts retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Facebook posts for brand {BrandId}", brandId);
                return StatusCode(500, GenericResponse<List<FacebookPostDetails>>.CreateError("Internal server error"));
            }
        }
    }
}
