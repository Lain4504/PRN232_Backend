using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Services.IServices;
using AISAM.Services.Service;
using AISAM.Common.Models;
using CommonUserResponseDto = AISAM.Common.Models.UserResponseDto;
using System.Security.Claims;
using AISAM.API.Validators;
using FluentValidation;
using FluentValidation.Results;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<UserController> _logger;
        private readonly ISocialService _socialService;
        private readonly IValidator<RegisterUserDto> _registerValidator;

        public UserController(IUserService userService, IJwtService jwtService, ILogger<UserController> logger, ISocialService socialService, IValidator<RegisterUserDto> registerValidator)
        {
            _userService = userService;
            _jwtService = jwtService;
            _logger = logger;
            _socialService = socialService;
            _registerValidator = registerValidator;
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<CommonUserResponseDto>>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return Unauthorized(GenericResponse<CommonUserResponseDto>.CreateError("Token không hợp lệ"));
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(GenericResponse<CommonUserResponseDto>.CreateError("Không tìm thấy người dùng"));
                }

                var response = new CommonUserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    Username = user.Username ?? "",
                    CreatedAt = user.CreatedAt,
                    SocialAccounts = user.SocialAccounts?.Select(sa => new SocialAccountDto
                    {
                        Id = sa.Id,
                        Provider = sa.Provider,
                        ProviderUserId = sa.ProviderUserId,
                        CreatedAt = sa.CreatedAt,
                        Targets = sa.SocialTargets?.Select(st => new SocialTargetDto
                        {
                            Id = st.Id,
                            ProviderTargetId = st.ProviderTargetId,
                            Name = st.Name,
                            Type = st.Type
                        }).ToList() ?? new List<SocialTargetDto>()
                    }).ToList() ?? new List<SocialAccountDto>()
                };

                return Ok(GenericResponse<CommonUserResponseDto>.CreateSuccess(response, "Lấy thông tin thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, GenericResponse<CommonUserResponseDto>.CreateError(
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
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
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
                    Provider = sa.Provider,
                    ProviderUserId = sa.ProviderUserId,
                    CreatedAt = sa.CreatedAt,
                    Targets = sa.SocialTargets?.Select(st => new SocialTargetDto
                    {
                        Id = st.Id,
                        ProviderTargetId = st.ProviderTargetId,
                        Name = st.Name,
                        Type = st.Type
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
    }
}
