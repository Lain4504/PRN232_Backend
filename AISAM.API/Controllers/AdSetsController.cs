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
    [Route("api/ad-sets")]
    [Authorize]
    public class AdSetsController : ControllerBase
    {
        private readonly IAdSetService _adSetService;
        private readonly ILogger<AdSetsController> _logger;

        public AdSetsController(
            IAdSetService adSetService,
            ILogger<AdSetsController> logger)
        {
            _adSetService = adSetService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<GenericResponse<AdSetResponse>>> CreateAdSet([FromBody] CreateAdSetRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adSetService.CreateAdSetAsync(userId, request);
                return Ok(GenericResponse<AdSetResponse>.CreateSuccess(result, "Ad set created successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdSetResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad set");
                return StatusCode(500, GenericResponse<AdSetResponse>.CreateError("Internal server error"));
            }
        }

        [HttpGet("campaign/{campaignId}")]
        public async Task<ActionResult<GenericResponse<List<AdSetResponse>>>> GetAdSetsByCampaign(Guid campaignId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adSetService.GetAdSetsByCampaignAsync(userId, campaignId);
                return Ok(GenericResponse<List<AdSetResponse>>.CreateSuccess(result, "Ad sets retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<List<AdSetResponse>>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad sets for campaign {CampaignId}", campaignId);
                return StatusCode(500, GenericResponse<List<AdSetResponse>>.CreateError("Internal server error"));
            }
        }

        [HttpGet("{adSetId}")]
        public async Task<ActionResult<GenericResponse<AdSetResponse>>> GetAdSetById(Guid adSetId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adSetService.GetAdSetByIdAsync(userId, adSetId);
                
                if (result == null)
                {
                    return NotFound(GenericResponse<AdSetResponse>.CreateError("Ad set not found"));
                }

                return Ok(GenericResponse<AdSetResponse>.CreateSuccess(result, "Ad set retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdSetResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad set {AdSetId}", adSetId);
                return StatusCode(500, GenericResponse<AdSetResponse>.CreateError("Internal server error"));
            }
        }
    }
}
