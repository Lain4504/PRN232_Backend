using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<TeamMemberResponseDto>>> GetById(Guid id)
        {
            try
            {
                var member = await _teamMemberService.GetByIdAsync(id);
                if (member == null)
                    return NotFound(GenericResponse<TeamMemberResponseDto>.CreateError("Không tìm thấy thành viên"));

                return Ok(GenericResponse<TeamMemberResponseDto>.CreateSuccess(member, "Lấy thành viên thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team member {MemberId}", id);
                return StatusCode(500, GenericResponse<TeamMemberResponseDto>.CreateError("Lỗi khi lấy thành viên"));
            }
        }

        [HttpGet]
        //[Authorize]
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
                return StatusCode(500, GenericResponse<PagedResult<TeamMemberResponseDto>>.CreateError("Lỗi khi lấy danh sách"));
            }
        }

        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<GenericResponse<TeamMemberResponseDto>>> Create([FromBody] TeamMemberCreateRequest request)
        {
            try
            {
                var created = await _teamMemberService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, GenericResponse<TeamMemberResponseDto>.CreateSuccess(created, "Tạo thành viên thành công"));
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

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<TeamMemberResponseDto>>> Update(Guid id, [FromBody] TeamMemberUpdateRequest request)
        {
            try
            {
                var updated = await _teamMemberService.UpdateAsync(id, request);
                if (updated == null)
                    return NotFound(GenericResponse<TeamMemberResponseDto>.CreateError("Không tìm thấy thành viên"));

                return Ok(GenericResponse<TeamMemberResponseDto>.CreateSuccess(updated, "Cập nhật thành viên thành công"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request updating member {MemberId}", id);
                return BadRequest(GenericResponse<TeamMemberResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team member {MemberId}", id);
                return StatusCode(500, GenericResponse<TeamMemberResponseDto>.CreateError("Lỗi khi cập nhật"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> Delete(Guid id)
        {
            try
            {
                var ok = await _teamMemberService.DeleteAsync(id);
                if (!ok)
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy thành viên"));

                return Ok(GenericResponse<object>.CreateSuccess(null, "Xóa thành viên thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team member {MemberId}", id);
                return StatusCode(500, GenericResponse<object>.CreateError("Lỗi khi xóa thành viên"));
            }
        }
    }
}
