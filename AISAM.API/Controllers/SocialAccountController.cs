using AISAM.API.Utils;
using AISAM.Common;
using AISAM.Common.Models;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISAM.API.Controllers;

[ApiController]
[Route("api/social/accounts")]
public class SocialAccountsController : ControllerBase
{
    private readonly ILogger<SocialAccountsController> _logger;
    private readonly ISocialService _socialService;
    private readonly IUserService _userService;

    public SocialAccountsController(
        ISocialService socialService,
        IUserService userService,
        ILogger<SocialAccountsController> logger)
    {
        _socialService = socialService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    ///     List available targets (e.g., Facebook pages) for the linked account of current user
    /// </summary>
    [HttpGet("{provider}/available-targets")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<AvailableTargetsResponse>>> ListAvailableTargets(string provider)
    {
        try
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var targets = await _socialService.ListAvailableTargetsAsync(userId, provider);
            return Ok(new GenericResponse<AvailableTargetsResponse>
            {
                Success = true,
                Data = new AvailableTargetsResponse { Targets = targets.ToList() }
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new GenericResponse<AvailableTargetsResponse>
                { Success = false, Message = "Invalid user context" });
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
            return StatusCode(500,
                new GenericResponse<AvailableTargetsResponse> { Success = false, Message = "Internal server error" });
        }
    }

    /// <summary>
    ///     Link selected targets to the user's linked social account
    /// </summary>
    [HttpPost("link-selected")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<SocialAccountDto>>> LinkSelectedTargets(
        [FromBody] LinkSelectedTargetsRequest request)
    {
        try
        {
            var authenticatedUserId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (authenticatedUserId != request.UserId) return Forbid();

            var result =
                await _socialService.LinkSelectedTargetsAsync(request.UserId, request.Provider,
                    request.ProviderTargetIds);
            return Ok(new GenericResponse<SocialAccountDto> { Success = true, Data = result });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new GenericResponse<SocialAccountDto>
                { Success = false, Message = "Invalid user context" });
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
            return StatusCode(500,
                new GenericResponse<SocialAccountDto> { Success = false, Message = "Internal server error" });
        }
    }

    /// <summary>
    ///     Unlink social account from user
    /// </summary>
    [HttpDelete("unlink/{userId}/{socialAccountId}")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<object>>> UnlinkAccount(
        Guid userId,
        Guid socialAccountId)
    {
        try
        {
            var success = await _socialService.UnlinkAccountAsync(userId, socialAccountId);
            if (success)
                return Ok(new GenericResponse<object>
                {
                    Success = true,
                    Message = "Social account unlinked successfully"
                });

            return NotFound(new GenericResponse<object>
            {
                Success = false,
                Message = "Social account not found or doesn't belong to user"
            });
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

    /// <summary>
    ///     Unlink a linked page/target from the user's social account
    /// </summary>
    [HttpDelete("unlink-target/{userId}/{socialIntegrationId}")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<object>>> UnlinkTarget(
        Guid userId,
        Guid socialIntegrationId)
    {
        try
        {
            var success = await _socialService.UnlinkTargetAsync(userId, socialIntegrationId);
            if (success)
                return Ok(new GenericResponse<object>
                {
                    Success = true,
                    Message = "Target/page unlinked successfully"
                });

            return NotFound(new GenericResponse<object>
            {
                Success = false,
                Message = "Target not found or doesn't belong to user"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking target/page");
            return StatusCode(500, new GenericResponse<object>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    ///     Get all targets (pages/profiles) for a social account
    /// </summary>
    [HttpGet("account/{socialAccountId}")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<IEnumerable<SocialTargetDto>>>> GetAccountTargets(
        Guid socialAccountId)
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

    /// <summary>
    ///     Get all social accounts linked to the authenticated user
    /// </summary>
    [HttpGet("social-accounts/me")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<List<SocialAccountDto>>>> GetSocialAccounts()
    {
        try
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(GenericResponse<List<SocialAccountDto>>.CreateError("Không tìm thấy người dùng"));

            var socialAccounts = user.SocialAccounts.Select(sa => new SocialAccountDto
            {
                Id = sa.Id,
                Provider = sa.Platform.ToString().ToLower(),
                ProviderUserId = sa.AccountId ?? string.Empty,
                AccessToken = sa.UserAccessToken,
                IsActive = sa.IsActive,
                ExpiresAt = sa.ExpiresAt,
                CreatedAt = sa.CreatedAt,
                Targets = sa.SocialIntegrations.Select(si => new SocialTargetDto
                {
                    Id = si.Id,
                    ProviderTargetId = si.ExternalId ?? string.Empty,
                    Name = $"Page {si.ExternalId}",
                    Type = si.Platform.ToString().ToLower(),
                    IsActive = si.IsActive
                }).ToList()
            }).ToList();

            return Ok(GenericResponse<List<SocialAccountDto>>.CreateSuccess(
                socialAccounts,
                "Lấy danh sách tài khoản mạng xã hội thành công"
            ));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(GenericResponse<List<SocialAccountDto>>.CreateError("Token không hợp lệ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social accounts");
            return StatusCode(500, GenericResponse<List<SocialAccountDto>>.CreateError(
                "Đã xảy ra lỗi khi lấy danh sách tài khoản mạng xã hội"
            ));
        }
    }

    /// <summary>
    ///     Get all social accounts for a user
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