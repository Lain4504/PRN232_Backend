using AISAM.API.Utils;
using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/teams")]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly ILogger<TeamController> _logger;

        public TeamController(ITeamService teamService, ILogger<TeamController> logger)
        {
            _teamService = teamService;
            _logger = logger;
        }

        // GET api/teams/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<TeamResponseDto>>> GetById(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var team = await _teamService.GetByIdAsync(id, userId);
                if (team == null)
                    return NotFound(GenericResponse<TeamResponseDto>.CreateError("Không tìm thấy team hoặc không có quyền"));

                return Ok(GenericResponse<TeamResponseDto>.CreateSuccess(team, "Lấy team thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<TeamResponseDto>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team {TeamId}", id);
                return StatusCode(500, GenericResponse<TeamResponseDto>.CreateError("Đã xảy ra lỗi khi lấy team"));
            }
        }

        // GET api/teams
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<TeamResponseDto>>>> GetTeams(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var request = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _teamService.GetPagedAsync(userId, request);
                return Ok(GenericResponse<PagedResult<TeamResponseDto>>.CreateSuccess(result, "Lấy danh sách team thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<PagedResult<TeamResponseDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated teams");
                return StatusCode(500, GenericResponse<PagedResult<TeamResponseDto>>.CreateError("Đã xảy ra lỗi khi lấy danh sách team"));
            }
        }

        // POST api/teams
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GenericResponse<TeamResponseDto>>> Create([FromBody] CreateTeamRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var created = await _teamService.CreateAsync(userId, request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, GenericResponse<TeamResponseDto>.CreateSuccess(created, "Tạo team thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode((int)HttpStatusCode.Forbidden,
                    GenericResponse<TeamResponseDto>.CreateError(ex.Message, HttpStatusCode.Forbidden));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for team creation");
                return BadRequest(GenericResponse<TeamResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team");
                return StatusCode(500, GenericResponse<TeamResponseDto>.CreateError("Đã xảy ra lỗi khi tạo team"));
            }
        }

        // PUT api/teams/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<TeamResponseDto>>> Update(Guid id, [FromBody] UpdateTeamRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var updated = await _teamService.UpdateAsync(id, userId, request);
                if (updated == null)
                    return NotFound(GenericResponse<TeamResponseDto>.CreateError("Không tìm thấy team hoặc không có quyền"));

                return Ok(GenericResponse<TeamResponseDto>.CreateSuccess(updated, "Cập nhật team thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode((int)HttpStatusCode.Forbidden,
                    GenericResponse<TeamResponseDto>.CreateError(ex.Message, HttpStatusCode.Forbidden));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for team update {TeamId}", id);
                return BadRequest(GenericResponse<TeamResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team {TeamId}", id);
                return StatusCode(500, GenericResponse<TeamResponseDto>.CreateError("Đã xảy ra lỗi khi cập nhật team"));
            }
        }

        // DELETE api/teams/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> SoftDelete(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var ok = await _teamService.SoftDeleteAsync(id, userId);
                if (!ok)
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy team hoặc không có quyền"));

                return Ok(GenericResponse<object>.CreateSuccess(null, "Xóa team thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode((int)HttpStatusCode.Forbidden,
                    GenericResponse<object>.CreateError(ex.Message, HttpStatusCode.Forbidden));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team {TeamId}", id);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi khi xóa team"));
            }
        }
    }
}
