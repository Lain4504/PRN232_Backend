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
    [Route("api/ads")]
    [Authorize]
    public class AdsController : ControllerBase
    {
        private readonly IAdService _adService;
        private readonly ILogger<AdsController> _logger;

        public AdsController(
            IAdService adService,
            ILogger<AdsController> logger)
        {
            _adService = adService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<GenericResponse<AdResponse>>> CreateAd([FromBody] CreateAdRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adService.CreateAdAsync(userId, request);
                return Ok(GenericResponse<AdResponse>.CreateSuccess(result, "Ad created successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad");
                return StatusCode(500, GenericResponse<AdResponse>.CreateError("Internal server error"));
            }
        }

        [HttpGet]
        public async Task<ActionResult<GenericResponse<Common.Dtos.PagedResult<AdResponse>>>> GetAds(
            [FromQuery] Guid? campaignId,
            [FromQuery] Guid? brandId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adService.GetAdsAsync(userId, campaignId, brandId, page, pageSize);
                return Ok(GenericResponse<Common.Dtos.PagedResult<AdResponse>>.CreateSuccess(result, "Ads retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<Common.Dtos.PagedResult<AdResponse>>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ads");
                return StatusCode(500, GenericResponse<Common.Dtos.PagedResult<AdResponse>>.CreateError("Internal server error"));
            }
        }

        [HttpGet("{adId}")]
        public async Task<ActionResult<GenericResponse<AdResponse>>> GetAdById(Guid adId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adService.GetAdByIdAsync(userId, adId);
                
                if (result == null)
                {
                    return NotFound(GenericResponse<AdResponse>.CreateError("Ad not found"));
                }

                return Ok(GenericResponse<AdResponse>.CreateSuccess(result, "Ad retrieved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ad {AdId}", adId);
                return StatusCode(500, GenericResponse<AdResponse>.CreateError("Internal server error"));
            }
        }

        [HttpPut("{adId}/status")]
        public async Task<ActionResult<GenericResponse<object>>> UpdateAdStatus(Guid adId, [FromBody] UpdateAdStatusRequest request)
        {
            try
            {
                if (adId != request.AdId)
                {
                    return BadRequest(GenericResponse<object>.CreateError("Ad ID mismatch"));
                }

                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adService.UpdateAdStatusAsync(userId, request);
                
                if (!result)
                {
                    return NotFound(GenericResponse<object>.CreateError("Ad not found"));
                }

                return Ok(GenericResponse<object>.CreateSuccess(null, "Ad status updated successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ad status for ad {AdId}", adId);
                return StatusCode(500, GenericResponse<object>.CreateError("Internal server error"));
            }
        }

        [HttpDelete("{adId}")]
        public async Task<ActionResult<GenericResponse<object>>> DeleteAd(Guid adId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adService.DeleteAdAsync(userId, adId);
                
                if (!result)
                {
                    return NotFound(GenericResponse<object>.CreateError("Ad not found"));
                }

                return Ok(GenericResponse<object>.CreateSuccess(null, "Ad deleted successfully"));
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
                _logger.LogError(ex, "Error deleting ad {AdId}", adId);
                return StatusCode(500, GenericResponse<object>.CreateError("Internal server error"));
            }
        }

        [HttpPost("{adId}/pull-reports")]
        public async Task<ActionResult<GenericResponse<AdPerformanceResponse>>> PullReports(Guid adId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _adService.PullReportsAsync(userId, adId);
                
                if (result == null)
                {
                    return NotFound(GenericResponse<AdPerformanceResponse>.CreateError("Ad not found or no performance data available"));
                }

                return Ok(GenericResponse<AdPerformanceResponse>.CreateSuccess(result, "Reports pulled successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<AdPerformanceResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling reports for ad {AdId}", adId);
                return StatusCode(500, GenericResponse<AdPerformanceResponse>.CreateError("Internal server error"));
            }
        }
    }
}
