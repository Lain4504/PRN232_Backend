using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AISAM.API.Utils;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/team")]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        /// <summary>
        /// Tạo team mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<GenericResponse<TeamResponse>>> CreateTeam([FromBody] CreateTeamRequest request)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<TeamResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.CreateTeamAsync(request, profileId);

            if (result.Success)
            {
                return CreatedAtAction(nameof(GetTeamById), new { id = result.Data!.Id }, result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Lấy thông tin team theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<GenericResponse<TeamResponse>>> GetTeamById(Guid id)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<TeamResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.GetTeamByIdAsync(id, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Lấy danh sách teams theo vendor
        /// </summary>
        [HttpGet("vendor/{vendorId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<TeamResponse>>>> GetTeamsByVendor(Guid vendorId)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<IEnumerable<TeamResponse>>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.GetTeamsByProfileAsync(vendorId, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Cập nhật thông tin team
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<GenericResponse<TeamResponse>>> UpdateTeam(Guid id, [FromBody] CreateTeamRequest request)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<TeamResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.UpdateTeamAsync(id, request, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Xóa team
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<GenericResponse<bool>>> DeleteTeam(Guid id)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.DeleteTeamAsync(id, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Lấy danh sách thành viên trong team
        /// </summary>
        [HttpGet("{teamId}/members")]
        public async Task<ActionResult<GenericResponse<IEnumerable<TeamMemberResponseDto>>>> GetTeamMembers(Guid teamId)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.GetTeamMembersAsync(teamId, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Assign brands cho team
        /// </summary>
        [HttpPost("{id}/brands")]
        public async Task<ActionResult<GenericResponse<bool>>> AssignBrandToTeam(Guid id, [FromBody] AssignBrandToTeamRequest request)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.AssignBrandToTeamAsync(id, request, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Unassign brand khỏi team
        /// </summary>
        [HttpDelete("{id}/brands")]
        public async Task<ActionResult<GenericResponse<bool>>> UnassignBrandFromTeam(Guid id, [FromBody] UnassignBrandFromTeamRequest request)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.UnassignBrandFromTeamAsync(id, request, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Cập nhật trạng thái team
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<GenericResponse<bool>>> UpdateTeamStatus(Guid id, [FromBody] UpdateTeamStatusRequest request)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.UpdateTeamStatusAsync(id, request, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Khôi phục team đã bị xóa mềm
        /// </summary>
        [HttpPost("{id}/restore")]
        public async Task<ActionResult<GenericResponse<bool>>> RestoreTeam(Guid id)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.RestoreTeamAsync(id, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}