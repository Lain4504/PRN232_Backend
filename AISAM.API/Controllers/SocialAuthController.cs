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
                Guid? userId = null;
                if (UserClaimsHelper.TryGetUserId(User, out var parsed))
                {
                    userId = parsed;
                }

                var result = await _socialService.GetAuthUrlAsync(provider, state, userId);
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
        /// Handle OAuth callback and link social account to user (supports multiple Facebook accounts per user)
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
                // Validate request
                if (request == null || request.UserId == Guid.Empty)
                {
                    return BadRequest(GenericResponse<object>.CreateError(
                        "Invalid request data", 
                        System.Net.HttpStatusCode.BadRequest, 
                        "INVALID_REQUEST"));
                }

                var linkRequest = new LinkSocialAccountRequest
                {
                    UserId = request.UserId,
                    Provider = provider,
                    Code = request.Code,
                    State = request.State
                };

                var socialAccount = await _socialService.LinkAccountAsync(linkRequest);
                var user = await _userService.GetUserByIdAsync(request.UserId);

                // Get available targets (pages) for this newly linked account
                var availableTargets = await _socialService.ListAvailableTargetsForAccountAsync(socialAccount.Id);

                return Ok(GenericResponse<object>.CreateSuccess(new
                {
                    User = user,
                    SocialAccount = socialAccount,
                    AvailableTargets = availableTargets,
                    Message = $"Tài khoản {provider} đã được liên kết thành công. Bây giờ bạn có thể chọn các trang để liên kết."
                }, $"Tài khoản {provider} đã được liên kết thành công"));
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