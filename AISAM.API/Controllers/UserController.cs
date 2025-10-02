using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Services.IServices;
using AISAM.Common.Models;
using System.Security.Claims;
using AISAM.Common.Dtos.Response;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private readonly ISocialService _socialService;

        public UserController(IUserService userService, ILogger<UserController> logger, ISocialService socialService)
        {
            _userService = userService;
            _logger = logger;
            _socialService = socialService;
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<UserResponseDto>>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return Unauthorized(GenericResponse<UserResponseDto>.CreateError("Token không hợp lệ"));
                }

                // Get email from JWT claims
                var emailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                var email = emailClaim?.Value ?? "unknown@example.com";

                // Get or create user (sync with Supabase)
                var user = await _userService.GetOrCreateUserAsync(userId, email);

                var response = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    CreatedAt = user.CreatedAt,
                    SocialAccounts = user.SocialAccounts?.Select(sa => new SocialAccountDto
                    {
                        Id = sa.Id,
                        Provider = sa.Platform.ToString().ToLower(),
                        ProviderUserId = sa.AccountId ?? string.Empty,
                        AccessToken = sa.UserAccessToken,
                        IsActive = sa.IsActive,
                        ExpiresAt = sa.ExpiresAt,
                        CreatedAt = sa.CreatedAt,
                        Targets = sa.SocialIntegrations?.Select(si => new SocialTargetDto
                        {
                            Id = si.Id,
                            ProviderTargetId = si.ExternalId ?? string.Empty,
                            Name = $"Page {si.ExternalId}",
                            Type = si.Platform.ToString().ToLower(),
                            IsActive = si.IsActive
                        }).ToList() ?? new List<SocialTargetDto>()
                    }).ToList() ?? new List<SocialAccountDto>()
                };

                return Ok(GenericResponse<UserResponseDto>.CreateSuccess(response, "Lấy thông tin thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, GenericResponse<UserResponseDto>.CreateError(
                    "Đã xảy ra lỗi khi lấy thông tin người dùng"
                ));
            }
        }

        [HttpGet("social-accounts")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<List<SocialAccountDto>>>> GetSocialAccounts()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return Unauthorized(GenericResponse<List<SocialAccountDto>>.CreateError("Token không hợp lệ"));
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(GenericResponse<List<SocialAccountDto>>.CreateError("Không tìm thấy người dùng"));
                }

                var socialAccounts = user.SocialAccounts?.Select(sa => new SocialAccountDto
                {
                    Id = sa.Id,
                    Provider = sa.Platform.ToString().ToLower(),
                    ProviderUserId = sa.AccountId ?? string.Empty,
                    AccessToken = sa.UserAccessToken,
                    IsActive = sa.IsActive,
                    ExpiresAt = sa.ExpiresAt,
                    CreatedAt = sa.CreatedAt,
                    Targets = sa.SocialIntegrations?.Select(si => new SocialTargetDto
                    {
                        Id = si.Id,
                        ProviderTargetId = si.ExternalId ?? string.Empty,
                        Name = $"Page {si.ExternalId}",
                        Type = si.Platform.ToString().ToLower(),
                        IsActive = si.IsActive
                    }).ToList() ?? new List<SocialTargetDto>()
                }).ToList() ?? new List<SocialAccountDto>();

                return Ok(GenericResponse<List<SocialAccountDto>>.CreateSuccess(
                    socialAccounts, 
                    "Lấy danh sách tài khoản mạng xã hội thành công"
                ));
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
        /// Get all targets (pages/profiles) for a social account
        /// </summary>
        [HttpGet("account/{socialAccountId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<SocialTargetDto>>>> GetAccountTargets(Guid socialAccountId)
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
        /// Get paginated list of users
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<GenericResponse<PagedResult<UserListDto>>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false)
        {
            try
            {
                var request = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _userService.GetPagedUsersAsync(request);
                return Ok(GenericResponse<PagedResult<UserListDto>>.CreateSuccess(result, "Lấy danh sách người dùng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated users");
                return StatusCode(500, GenericResponse<PagedResult<UserListDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy danh sách người dùng"
                ));
            }
        }
    }
}