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
    [Route("api/ad-campaigns")]
    [Authorize]
    public class AdCampaignsController : ControllerBase
    {
        private readonly IAdCampaignService _adCampaignService;
        private readonly ILogger<AdCampaignsController> _logger;

        public AdCampaignsController(
            IAdCampaignService adCampaignService,
            ILogger<AdCampaignsController> logger)
        {
            _adCampaignService = adCampaignService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<GenericResponse<AdCampaignResponse>>> CreateCampaign([FromBody] CreateAdCampaignRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCampaignService.CreateCampaignAsync(userId, request);
                return Ok(GenericResponse<AdCampaignResponse>.CreateSuccess(result, "Campaign created successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdCampaignResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad campaign");
                return StatusCode(500, GenericResponse<AdCampaignResponse>.CreateError("Internal server error"));
            }
        }

        [HttpGet]
        public async Task<ActionResult<GenericResponse<Common.Dtos.PagedResult<AdCampaignResponse>>>> GetCampaigns(
            [FromQuery] Guid? brandId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCampaignService.GetCampaignsAsync(userId, brandId, page, pageSize);
                return Ok(GenericResponse<Common.Dtos.PagedResult<AdCampaignResponse>>.CreateSuccess(result, "Campaigns retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<Common.Dtos.PagedResult<AdCampaignResponse>>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad campaigns");
                return StatusCode(500, GenericResponse<Common.Dtos.PagedResult<AdCampaignResponse>>.CreateError("Internal server error"));
            }
        }

        [HttpGet("{campaignId}")]
        public async Task<ActionResult<GenericResponse<AdCampaignResponse>>> GetCampaignById(Guid campaignId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCampaignService.GetCampaignByIdAsync(userId, campaignId);
                
                if (result == null)
                {
                    return NotFound(GenericResponse<AdCampaignResponse>.CreateError("Campaign not found"));
                }

                return Ok(GenericResponse<AdCampaignResponse>.CreateSuccess(result, "Campaign retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdCampaignResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad campaign {CampaignId}", campaignId);
                return StatusCode(500, GenericResponse<AdCampaignResponse>.CreateError("Internal server error"));
            }
        }

        [HttpDelete("{campaignId}")]
        public async Task<ActionResult<GenericResponse<object>>> DeleteCampaign(Guid campaignId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adCampaignService.DeleteCampaignAsync(userId, campaignId);
                
                if (!result)
                {
                    return NotFound(GenericResponse<object>.CreateError("Campaign not found"));
                }

                return Ok(GenericResponse<object>.CreateSuccess(null, "Campaign deleted successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ad campaign {CampaignId}", campaignId);
                return StatusCode(500, GenericResponse<object>.CreateError("Internal server error"));
            }
        }
    }
}
