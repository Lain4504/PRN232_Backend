using BookStore.Common;
using BookStore.Common.Models;
using BookStore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("auth")]
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
                var result = await _socialService.GetAuthUrlAsync(provider, state);
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
        /// Handle OAuth callback and link social account to user
        /// </summary>
        [HttpGet("{provider}/callback")]
        public async Task<ActionResult<GenericResponse<object>>> HandleCallback(
            string provider,
            [FromQuery] string code,
            [FromQuery] string? state = null,
            [FromQuery] int? userId = null)
        {
            try
            {
                // For demo purposes, if no userId provided, create a demo user
                if (!userId.HasValue)
                {
                    var demoUser = await _userService.CreateUserAsync(
                        email: $"demo_{Guid.NewGuid().ToString("N")[..8]}@example.com",
                        username: $"user_{Guid.NewGuid().ToString("N")[..8]}"
                    );
                    userId = demoUser.Id;
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
                        Message = $"{provider} account linked successfully"
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
        /// Unlink social account from user
        /// </summary>
        [HttpDelete("unlink/{userId}/{socialAccountId}")]
        public async Task<ActionResult<GenericResponse<object>>> UnlinkAccount(
            int userId,
            int socialAccountId)
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