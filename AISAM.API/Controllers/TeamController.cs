using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AISAM.API.Utils;
using System.Linq;

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
        [Authorize]
        public async Task<ActionResult<GenericResponse<TeamResponse>>> CreateTeam([FromBody] CreateTeamRequest request)
        {
            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
            if (profileId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<TeamResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<TeamResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.CreateTeamAsync(request, profileId, userId);

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

            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var result = await _teamService.GetTeamByIdAsync(id, profileId, userId);

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

            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var result = await _teamService.GetTeamsByProfileAsync(vendorId, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Cập nhật thông tin team (bao gồm tên, mô tả và trạng thái)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<GenericResponse<TeamResponse>>> UpdateTeam(Guid id, [FromBody] UpdateTeamRequest request)
        {
            if (request == null)
            {
                return BadRequest(GenericResponse<TeamResponse>.CreateError("Request body is required"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(GenericResponse<TeamResponse>.CreateError($"Validation failed: {string.Join(", ", errors)}"));
            }

            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<TeamResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.UpdateTeamAsync(id, request, userId);

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
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.DeleteTeamAsync(id, userId);

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

            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var result = await _teamService.GetTeamMembersAsync(teamId, profileId, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Lấy danh sách quyền của user hiện tại trong team
        /// </summary>
        [HttpGet("{teamId}/me/permissions")]
        public async Task<ActionResult<GenericResponse<IEnumerable<string>>>> GetMyPermissions(Guid teamId)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<IEnumerable<string>>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.GetMyPermissionsAsync(teamId, userId);
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
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.AssignBrandToTeamAsync(id, request, userId);

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
        /// Khôi phục team đã bị xóa mềm
        /// </summary>
        [HttpPost("{id}/restore")]
        public async Task<ActionResult<GenericResponse<bool>>> RestoreTeam(Guid id)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.RestoreTeamAsync(id, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Lấy danh sách teams mà user đang tham gia
        /// </summary>
        [HttpGet("user-teams")]
        public async Task<ActionResult<GenericResponse<IEnumerable<TeamResponse>>>> GetUserTeams()
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<IEnumerable<TeamResponse>>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.GetUserTeamsAsync(userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}