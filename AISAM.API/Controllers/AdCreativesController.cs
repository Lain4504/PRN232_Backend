using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using AISAM.API.Utils;
using Microsoft.Extensions.Logging;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/ad-creatives")]
    [Authorize]
    public class AdCreativesController : ControllerBase
    {
        private readonly IAdCreativeService _adCreativeService;
        private readonly ILogger<AdCreativesController> _logger;

        public AdCreativesController(
            IAdCreativeService adCreativeService,
            ILogger<AdCreativesController> logger)
        {
            _adCreativeService = adCreativeService;
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
    }
}
