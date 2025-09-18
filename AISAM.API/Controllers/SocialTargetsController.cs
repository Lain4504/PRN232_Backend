using BookStore.Common;
using BookStore.Common.Models;
using BookStore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/social/targets")]
    public class SocialTargetsController : ControllerBase
    {
        private readonly ISocialService _socialService;
        private readonly ILogger<SocialTargetsController> _logger;

        public SocialTargetsController(ISocialService socialService, ILogger<SocialTargetsController> logger)
        {
            _socialService = socialService;
            _logger = logger;
        }

        /// <summary>
        /// Get all targets (pages/profiles) for a social account
        /// </summary>
        [HttpGet("account/{socialAccountId}")]
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


