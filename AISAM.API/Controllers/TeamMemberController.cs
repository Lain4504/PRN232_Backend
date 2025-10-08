using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using AISAM.API.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/team-members")]
    public class TeamMemberController : ControllerBase
    {
        private readonly ITeamMemberService _teamMemberService;
        private readonly ILogger<TeamMemberController> _logger;

        public TeamMemberController(ITeamMemberService teamMemberService, ILogger<TeamMemberController> logger)
        {
            _teamMemberService = teamMemberService;
            _logger = logger;
        }

        // ✅ Lấy danh sách phân trang
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<TeamMemberResponseDto>>>> GetPaged([FromQuery] PaginationRequest request)
        {
            try
            {
                var result = await _teamMemberService.GetPagedAsync(request);
                return Ok(GenericResponse<PagedResult<TeamMemberResponseDto>>.CreateSuccess(result, "Lấy danh sách thành viên thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team members");
                return StatusCode(500, GenericResponse<PagedResult<TeamMemberResponseDto>>.CreateError("Lỗi khi lấy danh sách thành viên"));
            }
        }

        // ✅ Lấy chi tiết member
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<TeamMemberResponseDto>>> GetById(Guid id)
        {
            try
            {
                // Lấy userId từ JWT token
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var member = await _teamMemberService.GetByIdAsync(id, userId);
                if (member == null)
                    return NotFound(GenericResponse<TeamMemberResponseDto>.CreateError("Không tìm thấy thành viên"));

                return Ok(GenericResponse<TeamMemberResponseDto>.CreateSuccess(member, "Lấy chi tiết thành viên thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Không có quyền xem chi tiết thành viên");
                return Forbid(GenericResponse<TeamMemberResponseDto>.CreateError(ex.Message).Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết team member");
                return StatusCode(500, GenericResponse<TeamMemberResponseDto>.CreateError("Lỗi hệ thống"));
            }
        }

        // ✅ Tạo team member
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GenericResponse<TeamMemberResponseDto>>> Create([FromBody] TeamMemberCreateRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var created = await _teamMemberService.CreateAsync(request, userId);

                return CreatedAtAction(nameof(GetById), new { id = created.Id },
                    GenericResponse<TeamMemberResponseDto>.CreateSuccess(created, "Tạo thành viên thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Không có quyền thêm thành viên");
                return Forbid(GenericResponse<TeamMemberResponseDto>.CreateError(ex.Message).Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request creating team member");
                return BadRequest(GenericResponse<TeamMemberResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team member");
                return StatusCode(500, GenericResponse<TeamMemberResponseDto>.CreateError("Lỗi khi tạo thành viên"));
            }
        }

        // ✅ Cập nhật team member
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<TeamMemberResponseDto>>> Update(Guid id, [FromBody] TeamMemberUpdateRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var updated = await _teamMemberService.UpdateAsync(id, request, userId);

                if (updated == null)
                    return NotFound(GenericResponse<TeamMemberResponseDto>.CreateError("Không tìm thấy thành viên"));

                return Ok(GenericResponse<TeamMemberResponseDto>.CreateSuccess(updated, "Cập nhật thành viên thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Không có quyền cập nhật thành viên");
                return Forbid(GenericResponse<TeamMemberResponseDto>.CreateError(ex.Message).Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request updating member {MemberId}", id);
                return BadRequest(GenericResponse<TeamMemberResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team member {MemberId}", id);
                return StatusCode(500, GenericResponse<TeamMemberResponseDto>.CreateError("Lỗi hệ thống"));
            }
        }

        // ✅ Xóa team member
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> Delete(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var ok = await _teamMemberService.DeleteAsync(id, userId);

                if (!ok)
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy thành viên"));

                return Ok(GenericResponse<object>.CreateSuccess(null, "Xóa thành viên thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Không có quyền xóa thành viên");
                return Forbid(GenericResponse<object>.CreateError(ex.Message).Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team member {MemberId}", id);
                return StatusCode(500, GenericResponse<object>.CreateError("Lỗi hệ thống"));
            }
        }
    }
}
