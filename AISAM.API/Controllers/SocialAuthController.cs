using AISAM.Common;
using AISAM.Common.Models;
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
        /// Handle OAuth callback and link social account to user (do not auto-link pages)
        /// </summary>
        [HttpGet("{provider}/callback")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> HandleCallback(
            string provider,
            [FromQuery] string code,
            [FromQuery] string? state = null,
            [FromQuery] Guid? userId = null)
        {
            try
            {
                // Require userId; do not auto-create users when missing
                if (!userId.HasValue)
                {
                    return BadRequest(new GenericResponse<object>
                    {
                        Success = false,
                        Message = "userId is required"
                    });
                }

                var linkRequest = new LinkSocialAccountRequest
                {
                    UserId = userId.Value,
                    Provider = provider,
                    Code = code,
                    State = state
                };

                var socialAccount = await _socialService.LinkAccountAsync(linkRequest);
                var user = await _userService.GetUserByIdAsync(userId.Value);

                return Ok(new GenericResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        User = user,
                        SocialAccount = socialAccount,
                        Message = $"{provider} account linked successfully. Now select pages to link."
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new GenericResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Provider} callback", provider);
                return StatusCode(500, new GenericResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }
}