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
    ///     List available targets (e.g., Facebook pages) for a specific social account - for account management
    /// </summary>
    [HttpGet("{socialAccountId}/available-targets")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<AvailableTargetsResponse>>> GetAvailableTargetsForAccountManagement(Guid socialAccountId)
    {
        try
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

            // Verify the social account belongs to the user
            var socialAccount = await _socialService.GetSocialAccountByIdAsync(socialAccountId);
            if (socialAccount == null || socialAccount.ProfileId != profileId)
            {
                return NotFound(GenericResponse<AvailableTargetsResponse>.CreateError(
                    "Không tìm thấy tài khoản mạng xã hội", 
                    System.Net.HttpStatusCode.NotFound, 
                    "SOCIAL_ACCOUNT_NOT_FOUND"));
            }

            var targets = await _socialService.ListAvailableTargetsForAccountAsync(socialAccountId);
            return Ok(GenericResponse<AvailableTargetsResponse>.CreateSuccess(
                new AvailableTargetsResponse { Targets = targets.ToList() },
                "Lấy danh sách trang có sẵn thành công"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(GenericResponse<AvailableTargetsResponse>.CreateError(
                "Token không hợp lệ", 
                System.Net.HttpStatusCode.Unauthorized, 
                "UNAUTHORIZED"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(GenericResponse<AvailableTargetsResponse>.CreateError(
                ex.Message, 
                System.Net.HttpStatusCode.BadRequest, 
                "INVALID_REQUEST"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(GenericResponse<AvailableTargetsResponse>.CreateError(
                ex.Message, 
                System.Net.HttpStatusCode.BadRequest, 
                "FACEBOOK_OAUTH_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing available targets for social account {SocialAccountId}", socialAccountId);
            return StatusCode(500, GenericResponse<AvailableTargetsResponse>.CreateError(
                "Đã xảy ra lỗi hệ thống. Vui lòng thử lại.", 
                System.Net.HttpStatusCode.InternalServerError, 
                "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    ///     Link selected targets to a specific social account
    /// </summary>
    [HttpPost("{socialAccountId}/link-targets")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<SocialAccountDto>>> LinkSelectedTargets(
        Guid socialAccountId,
        [FromBody] LinkSelectedTargetsRequest request)
    {
        try
        {
            var authenticatedUserId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (authenticatedUserId != request.ProfileId) return Forbid();

            // Verify the social account belongs to the user
            var socialAccount = await _socialService.GetSocialAccountByIdAsync(socialAccountId);
            if (socialAccount == null || socialAccount.ProfileId != request.ProfileId)
            {
                return NotFound(GenericResponse<SocialAccountDto>.CreateError(
                    "Không tìm thấy tài khoản mạng xã hội", 
                    System.Net.HttpStatusCode.NotFound, 
                    "SOCIAL_ACCOUNT_NOT_FOUND"));
            }

            var result = await _socialService.LinkSelectedTargetsForAccountAsync(socialAccountId, request);
            return Ok(GenericResponse<SocialAccountDto>.CreateSuccess(result, "Liên kết các trang đã chọn thành công"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(GenericResponse<SocialAccountDto>.CreateError(
                "Token không hợp lệ", 
                System.Net.HttpStatusCode.Unauthorized, 
                "UNAUTHORIZED"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(GenericResponse<SocialAccountDto>.CreateError(
                ex.Message, 
                System.Net.HttpStatusCode.BadRequest, 
                "INVALID_REQUEST"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(GenericResponse<SocialAccountDto>.CreateError(
                ex.Message, 
                System.Net.HttpStatusCode.BadRequest, 
                "FACEBOOK_OAUTH_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking selected targets for social account {SocialAccountId}", socialAccountId);
            return StatusCode(500, GenericResponse<SocialAccountDto>.CreateError(
                "Đã xảy ra lỗi hệ thống. Vui lòng thử lại.", 
                System.Net.HttpStatusCode.InternalServerError, 
                "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    ///     Unlink social account from user
    /// </summary>
    [HttpDelete("unlink/{profileId}/{socialAccountId}")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<object>>> UnlinkAccount(
        Guid profileId,
        Guid socialAccountId)
    {
        try
        {
            var success = await _socialService.UnlinkAccountAsync(profileId, socialAccountId);
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
    [HttpDelete("unlink-target/{profileId}/{socialIntegrationId}")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<object>>> UnlinkTarget(
        Guid profileId,
        Guid socialIntegrationId)
    {
        try
        {
            var success = await _socialService.UnlinkTargetAsync(profileId, socialIntegrationId);
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
    ///     Get all social accounts linked to the authenticated user
    /// </summary>
    [HttpGet("social-accounts/me")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<List<SocialAccountDto>>>> GetSocialAccounts()
    {
        try
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

            var user = await _userService.GetUserByIdAsync(profileId);
            if (user == null)
                return NotFound(GenericResponse<List<SocialAccountDto>>.CreateError("Không tìm thấy người dùng"));

            // Get user's profiles and their social accounts
            var socialAccounts = new List<SocialAccountDto>();
            foreach (var profile in user.Profiles)
            {
                var profileSocialAccounts = profile.SocialAccounts.Select(sa => new SocialAccountDto
                {
                    Id = sa.Id,
                    ProfileId = sa.ProfileId,
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
                
                socialAccounts.AddRange(profileSocialAccounts);
            }

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
    [HttpGet("user/{profileId}")]
    public async Task<ActionResult<GenericResponse<IEnumerable<SocialAccountDto>>>> GetUserAccounts(Guid profileId)
    {
        try
        {
            var accounts = await _socialService.GetProfileAccountsAsync(profileId);
            return Ok(GenericResponse<IEnumerable<SocialAccountDto>>.CreateSuccess(
                accounts, 
                "Lấy danh sách tài khoản mạng xã hội thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user accounts for user {UserId}", profileId);
            return StatusCode(500, GenericResponse<IEnumerable<SocialAccountDto>>.CreateError(
                "Đã xảy ra lỗi hệ thống. Vui lòng thử lại.", 
                System.Net.HttpStatusCode.InternalServerError, 
                "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    ///     Get all linked targets (pages) for a specific social account
    /// </summary>
    [HttpGet("{socialAccountId}/linked-targets")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<IEnumerable<SocialTargetDto>>>> GetLinkedTargets(Guid socialAccountId)
    {
        try
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

            // Verify the social account belongs to the user
            var socialAccount = await _socialService.GetSocialAccountByIdAsync(socialAccountId);
            if (socialAccount == null || socialAccount.ProfileId != profileId)
            {
                return NotFound(GenericResponse<IEnumerable<SocialTargetDto>>.CreateError(
                    "Không tìm thấy tài khoản mạng xã hội", 
                    System.Net.HttpStatusCode.NotFound, 
                    "SOCIAL_ACCOUNT_NOT_FOUND"));
            }

            var targets = await _socialService.GetAccountTargetsAsync(socialAccountId);
            return Ok(GenericResponse<IEnumerable<SocialTargetDto>>.CreateSuccess(
                targets, 
                "Lấy danh sách trang đã liên kết thành công"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(GenericResponse<IEnumerable<SocialTargetDto>>.CreateError(
                "Token không hợp lệ", 
                System.Net.HttpStatusCode.Unauthorized, 
                "UNAUTHORIZED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting targets for social account {SocialAccountId}", socialAccountId);
            return StatusCode(500, GenericResponse<IEnumerable<SocialTargetDto>>.CreateError(
                "Đã xảy ra lỗi hệ thống. Vui lòng thử lại.", 
                System.Net.HttpStatusCode.InternalServerError, 
                "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    ///     Get all social accounts with their targets for the authenticated user
    /// </summary>
    [HttpGet("me/accounts-with-targets")]
    [Authorize]
    public async Task<ActionResult<GenericResponse<List<SocialAccountWithTargetsDto>>>> GetMyAccountsWithTargets()
    {
        try
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            var accounts = await _socialService.GetProfileAccountsAsync(profileId);
            
            var accountsWithTargets = new List<SocialAccountWithTargetsDto>();
            
            foreach (var account in accounts)
            {
                var targets = await _socialService.GetAccountTargetsAsync(account.Id);
                accountsWithTargets.Add(new SocialAccountWithTargetsDto
                {
                    SocialAccount = account,
                    Targets = targets.ToList()
                });
            }

            return Ok(GenericResponse<List<SocialAccountWithTargetsDto>>.CreateSuccess(
                accountsWithTargets, 
                "Lấy danh sách tài khoản và trang thành công"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(GenericResponse<List<SocialAccountWithTargetsDto>>.CreateError(
                "Token không hợp lệ", 
                System.Net.HttpStatusCode.Unauthorized, 
                "UNAUTHORIZED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts with targets for user");
            return StatusCode(500, GenericResponse<List<SocialAccountWithTargetsDto>>.CreateError(
                "Đã xảy ra lỗi hệ thống. Vui lòng thử lại.", 
                System.Net.HttpStatusCode.InternalServerError, 
                "INTERNAL_SERVER_ERROR"));
        }
    }

}