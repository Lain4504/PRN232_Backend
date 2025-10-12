using AISAM.Common.DTOs.Request;
using AISAM.Common.DTOs.Response;
using AISAM.Common;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Dtos.Request;
using System.Security.Claims;
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
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<TeamResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.CreateTeamAsync(request, userId);

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
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<TeamResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.GetTeamByIdAsync(id, userId);

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
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<IEnumerable<TeamResponse>>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.GetTeamsByVendorAsync(vendorId, userId);

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
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _teamService.GetTeamMembersAsync(teamId, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

    }
}