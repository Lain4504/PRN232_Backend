using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Services.IServices;
using AISAM.Common.Dtos.Response;
using AISAM.API.Utils;
using AISAM.Common.Dtos;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("profile/me")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<UserResponseDto>>> GetProfile()
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(GenericResponse<UserResponseDto>.CreateError("Không tìm thấy người dùng"));
                }

                var response = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    CreatedAt = user.CreatedAt
                };

                return Ok(GenericResponse<UserResponseDto>.CreateSuccess(response, "Lấy thông tin thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<UserResponseDto>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, GenericResponse<UserResponseDto>.CreateError(
                    "Đã xảy ra lỗi khi lấy thông tin người dùng"
                ));
            }
        }

        /// <summary>
        /// Get paginated list of users
        /// </summary>
        [HttpGet]
        [Authorize]
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