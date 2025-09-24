using AISAM.Common;
using AISAM.Common.Models;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/social/accounts")]
    public class SocialAccountsController : ControllerBase
    {
        private readonly ISocialService _socialService;
        private readonly ILogger<SocialAccountsController> _logger;

        public SocialAccountsController(ISocialService socialService, ILogger<SocialAccountsController> logger)
        {
            _socialService = socialService;
            _logger = logger;
        }

        /// <summary>
        /// Get all social accounts for a user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<SocialAccountDto>>>> GetUserAccounts(Guid userId)
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
    }
}


