using AISAM.API.Utils;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISAM.API.Controllers;

[ApiController]
[Route("api/social/integrations")]
public class SocialIntegrationController : ControllerBase
{
    private readonly ILogger<SocialIntegrationController> _logger;
    private readonly ISocialService _socialService;

    public SocialIntegrationController(
        ISocialService socialService,
        ILogger<SocialIntegrationController> logger)
    {
        _socialService = socialService;
        _logger = logger;
    }

    /// <summary>
    /// Get social integrations by brand ID
    /// </summary>
    [HttpGet("brand/{brandId}")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<IEnumerable<SocialIntegrationDto>>>> GetByBrandId(Guid brandId)
    {
        try
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            var integrations = await _socialService.GetSocialIntegrationsByBrandIdAsync(brandId, profileId);
            return Ok(GenericResponse<IEnumerable<SocialIntegrationDto>>.CreateSuccess(integrations));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to social integrations for brand {BrandId}", brandId);
            return StatusCode(403, GenericResponse<IEnumerable<SocialIntegrationDto>>.CreateError(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social integrations for brand {BrandId}", brandId);
            return StatusCode(500, GenericResponse<IEnumerable<SocialIntegrationDto>>.CreateError("Lỗi hệ thống"));
        }
    }

    /// <summary>
    /// Link Facebook ad account to social integration
    /// </summary>
    [HttpPost("{socialIntegrationId}/link-ad-account")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<object>>> LinkAdAccount(
        Guid socialIntegrationId, 
        [FromBody] LinkAdAccountRequest request)
    {
        try
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

            if (string.IsNullOrEmpty(request.AdAccountId))
            {
                return BadRequest(GenericResponse<object>.CreateError(
                    "AdAccountId is required", 
                    System.Net.HttpStatusCode.BadRequest, 
                    "INVALID_REQUEST"));
            }

            var success = await _socialService.LinkAdAccountToIntegrationAsync(socialIntegrationId, request.AdAccountId);
            
            if (success)
            {
                return Ok(GenericResponse<object>.CreateSuccess(
                    new { Message = "Ad account linked successfully" },
                    "Liên kết ad account thành công"
                ));
            }
            else
            {
                return NotFound(GenericResponse<object>.CreateError(
                    "Social integration not found", 
                    System.Net.HttpStatusCode.NotFound, 
                    "INTEGRATION_NOT_FOUND"));
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(GenericResponse<object>.CreateError(
                "Token không hợp lệ", 
                System.Net.HttpStatusCode.Unauthorized, 
                "UNAUTHORIZED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking ad account to social integration {SocialIntegrationId}", socialIntegrationId);
            return StatusCode(500, GenericResponse<object>.CreateError(
                "Đã xảy ra lỗi hệ thống. Vui lòng thử lại.", 
                System.Net.HttpStatusCode.InternalServerError, 
                "INTERNAL_SERVER_ERROR"));
        }
    }
}