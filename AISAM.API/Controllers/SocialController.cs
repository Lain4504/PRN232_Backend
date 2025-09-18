using BookStore.Common;
using BookStore.Common.Models;
using BookStore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/social")]
    public class SocialController : ControllerBase
    {
        private readonly ISocialService _socialService;
        private readonly ILogger<SocialController> _logger;

        public SocialController(ISocialService socialService, ILogger<SocialController> logger)
        {
            _socialService = socialService;
            _logger = logger;
        }

        /// <summary>
        /// Get all social accounts for a user
        /// </summary>
        [HttpGet("accounts/user/{userId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<SocialAccountDto>>>> GetUserAccounts(int userId)
        {
            try
            {
                var accounts = await _socialService.GetUserAccountsAsync(userId);
                return Ok(new GenericResponse<IEnumerable<SocialAccountDto>>
                {
                    Success = true,
                    Data = accounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user accounts for user {UserId}", userId);
                return StatusCode(500, new GenericResponse<IEnumerable<SocialAccountDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get all targets (pages/profiles) for a social account
        /// </summary>
        [HttpGet("targets/account/{socialAccountId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<SocialTargetDto>>>> GetAccountTargets(int socialAccountId)
        {
            try
            {
                var targets = await _socialService.GetAccountTargetsAsync(socialAccountId);
                return Ok(new GenericResponse<IEnumerable<SocialTargetDto>>
                {
                    Success = true,
                    Data = targets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting targets for account {AccountId}", socialAccountId);
                return StatusCode(500, new GenericResponse<IEnumerable<SocialTargetDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        
    }
}