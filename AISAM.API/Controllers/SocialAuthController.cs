using AISAM.Common;
using AISAM.Common.Models;
using AISAM.Common.Dtos.Request;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using AISAM.Common.Dtos.Response;
using AISAM.API.Utils;
using Microsoft.AspNetCore.Authorization;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/social-auth")]
    public class SocialAuthController : ControllerBase
    {
        private readonly ISocialService _socialService;
        private readonly IUserService _userService;
        private readonly ILogger<SocialAuthController> _logger;

        public SocialAuthController(
            ISocialService socialService,
            IUserService userService,
            ILogger<SocialAuthController> logger)
        {
            _socialService = socialService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get OAuth authorization URL for a provider
        /// </summary>
        [HttpGet("{provider}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<AuthUrlResponse>>> GetAuthUrl(
            string provider,
            [FromQuery] string? state = null)
        {
            try
            {
                var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

                var result = await _socialService.GetAuthUrlAsync(provider, state, profileId);
                return Ok(new GenericResponse<AuthUrlResponse>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<AuthUrlResponse>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auth URL for provider {Provider}", provider);
                return StatusCode(500, new GenericResponse<AuthUrlResponse>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Handle OAuth callback and link social account to profile (supports multiple Facebook accounts per profile)
        /// This endpoint is called by the frontend after OAuth redirect
        /// </summary>
        [HttpPost("{provider}/callback")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> HandleCallback(
            string provider,
            [FromBody] SocialCallbackRequest request)
        {
            try
            {
                // Get current profile ID from context
                var currentProfileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
                
                // Validate request
                if (request == null || string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest(GenericResponse<object>.CreateError(
                        "Invalid request data", 
                        System.Net.HttpStatusCode.BadRequest, 
                        "INVALID_REQUEST"));
                }

                var linkRequest = new LinkSocialAccountRequest
                {
                    ProfileId = currentProfileId,
                    Provider = provider,
                    Code = request.Code,
                    State = request.State
                };

                var socialAccount = await _socialService.LinkAccountAsync(linkRequest);
                // Profile information is already validated in SocialService.LinkAccountAsync
                // LinkAccountAsync will create new account or update existing one (re-auth)

                // Get available targets (pages) for this account
                var availableTargets = await _socialService.ListAvailableTargetsForAccountAsync(socialAccount.Id);

                // Check if account was just created or updated (re-auth)
                // If UpdatedAt is very close to CreatedAt (within 2 seconds), it's likely a new account
                // If UpdatedAt is significantly later than CreatedAt, it's a re-auth
                var timeDiff = socialAccount.UpdatedAt - socialAccount.CreatedAt;
                var isReAuth = timeDiff.TotalSeconds > 2;
                var message = isReAuth
                    ? $"Tài khoản {provider} đã được xác thực lại thành công. Token đã được cập nhật."
                    : $"Tài khoản {provider} đã được liên kết thành công. Bây giờ bạn có thể chọn các trang để liên kết.";

                return Ok(GenericResponse<object>.CreateSuccess(new
                {
                    SocialAccount = socialAccount,
                    AvailableTargets = availableTargets,
                    Message = message,
                    IsReAuth = isReAuth
                }, isReAuth ? $"Tài khoản {provider} đã được xác thực lại thành công" : $"Tài khoản {provider} đã được liên kết thành công"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest, "INVALID_REQUEST"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest, "FACEBOOK_OAUTH_ERROR"));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error handling {Provider} callback", provider);
                return BadRequest(GenericResponse<object>.CreateError("Lỗi kết nối với Facebook. Vui lòng thử lại.", System.Net.HttpStatusCode.BadRequest, "FACEBOOK_CONNECTION_ERROR"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Provider} callback", provider);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi hệ thống. Vui lòng thử lại.", System.Net.HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR"));
            }
        }
    }
}