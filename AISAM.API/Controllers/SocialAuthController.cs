using AISAM.Common;
using AISAM.Common.Models;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        public async Task<ActionResult<GenericResponse<AuthUrlResponse>>> GetAuthUrl(
            string provider,
            [FromQuery] string? state = null)
        {
            try
            {
                Guid? userId = null;
                var nameId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
                if (Guid.TryParse(nameId, out var parsed))
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

        /// <summary>
        /// List available targets (e.g., Facebook pages) for the linked account of current user
        /// </summary>
        [HttpGet("{provider}/available-targets")]
        public async Task<ActionResult<GenericResponse<AvailableTargetsResponse>>> ListAvailableTargets(string provider)
        {
            try
            {
                var nameId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
                if (!Guid.TryParse(nameId, out var userId))
                {
                    return Unauthorized(new GenericResponse<AvailableTargetsResponse> { Success = false, Message = "Invalid user context" });
                }

                var targets = await _socialService.ListAvailableTargetsAsync(userId, provider);
                return Ok(new GenericResponse<AvailableTargetsResponse>
                {
                    Success = true,
                    Data = new AvailableTargetsResponse { Targets = targets.ToList() }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<AvailableTargetsResponse> { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new GenericResponse<AvailableTargetsResponse> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing available targets for {Provider}", provider);
                return StatusCode(500, new GenericResponse<AvailableTargetsResponse> { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Link selected targets to the user's linked social account
        /// </summary>
        [HttpPost("link-selected")]
        public async Task<ActionResult<GenericResponse<SocialAccountDto>>> LinkSelectedTargets([FromBody] LinkSelectedTargetsRequest request)
        {
            try
            {
                var nameId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
                if (!Guid.TryParse(nameId, out var authenticatedUserId) || authenticatedUserId != request.UserId)
                {
                    return Forbid();
                }

                var result = await _socialService.LinkSelectedTargetsAsync(request.UserId, request.Provider, request.ProviderTargetIds);
                return Ok(new GenericResponse<SocialAccountDto> { Success = true, Data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<SocialAccountDto> { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new GenericResponse<SocialAccountDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking selected targets");
                return StatusCode(500, new GenericResponse<SocialAccountDto> { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Link additional social account to existing user
        /// </summary>
        [HttpPost("link")]
        public async Task<ActionResult<GenericResponse<SocialAccountDto>>> LinkAccount(
            [FromBody] LinkSocialAccountRequest request)
        {
            try
            {
                var result = await _socialService.LinkAccountAsync(request);
                return Ok(new GenericResponse<SocialAccountDto>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<SocialAccountDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new GenericResponse<SocialAccountDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking social account");
                return StatusCode(500, new GenericResponse<SocialAccountDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// [TEMP] Link Facebook Page directly using Page Access Token for testing posting capability.
        /// This is a temporary endpoint and will be removed/replaced by the official OAuth flow later.
        /// </summary>
        [HttpPost("link-page-token")]
        public async Task<ActionResult<GenericResponse<SocialAccountDto>>> LinkPageByToken(
            [FromBody] LinkPageByTokenRequest request)
        {
            try
            {
                _logger.LogInformation("Linking Facebook page by token for user {UserId}", request.UserId);
                
                var result = await _socialService.LinkPageByTokenAsync(request);
                return Ok(new GenericResponse<SocialAccountDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Facebook page linked successfully"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid argument for link page by token: {Message}", ex.Message);
                return BadRequest(new GenericResponse<SocialAccountDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation for link page by token: {Message}", ex.Message);
                return BadRequest(new GenericResponse<SocialAccountDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking Facebook page by token for user {UserId}", request.UserId);
                return StatusCode(500, new GenericResponse<SocialAccountDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Unlink social account from user
        /// </summary>
        [HttpDelete("unlink/{userId}/{socialAccountId}")]
        public async Task<ActionResult<GenericResponse<object>>> UnlinkAccount(
            Guid userId,
            Guid socialAccountId)
        {
            try
            {
                var success = await _socialService.UnlinkAccountAsync(userId, socialAccountId);
                if (success)
                {
                    return Ok(new GenericResponse<object>
                    {
                        Success = true,
                        Message = "Social account unlinked successfully"
                    });
                }
                else
                {
                    return NotFound(new GenericResponse<object>
                    {
                        Success = false,
                        Message = "Social account not found or doesn't belong to user"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking social account");
                return StatusCode(500, new GenericResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }
}