using AISAM.Common.DTOs.Request;
using AISAM.Common.DTOs.Response;
using AISAM.Common;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            if (!ModelState.IsValid)
            {
                return BadRequest(GenericResponse<TeamResponse>.CreateError("Dữ liệu không hợp lệ"));
            }

            var userId = GetCurrentUserId();
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
            var userId = GetCurrentUserId();
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
            var userId = GetCurrentUserId();
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
            if (!ModelState.IsValid)
            {
                return BadRequest(GenericResponse<TeamResponse>.CreateError("Dữ liệu không hợp lệ"));
            }

            var userId = GetCurrentUserId();
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
            var userId = GetCurrentUserId();
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

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}