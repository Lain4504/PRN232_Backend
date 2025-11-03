using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AISAM.API.Utils;
using System.Linq;
using AISAM.Repositories.IRepositories;
using AISAM.Data.Enumeration;
using Microsoft.Extensions.Logging;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/team")]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IBrandService _brandService;
        private readonly IContentRepository _contentRepository;
        private readonly IPostRepository _postRepository;
        private readonly IApprovalRepository _approvalRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly ILogger<TeamController> _logger;

        public TeamController(
            ITeamService teamService,
            IBrandService brandService,
            IContentRepository contentRepository,
            IPostRepository postRepository,
            IApprovalRepository approvalRepository,
            ITeamMemberRepository teamMemberRepository,
            ILogger<TeamController> logger)
        {
            _teamService = teamService;
            _brandService = brandService;
            _contentRepository = contentRepository;
            _postRepository = postRepository;
            _approvalRepository = approvalRepository;
            _teamMemberRepository = teamMemberRepository;
            _logger = logger;
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

        /// <summary>
        /// Lấy thống kê tổng quan của team
        /// </summary>
        [HttpGet("{teamId}/stats")]
        public async Task<ActionResult<GenericResponse<TeamStatsResponse>>> GetTeamStats(Guid teamId)
        {
            try
            {
                var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
                if (profileId == Guid.Empty)
                {
                    return Unauthorized(GenericResponse<TeamStatsResponse>.CreateError("Không thể xác thực người dùng"));
                }

                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                if (userId == Guid.Empty)
                {
                    return Unauthorized(GenericResponse<TeamStatsResponse>.CreateError("Không thể xác thực người dùng"));
                }

                // Verify user has access to this team
                var teamResult = await _teamService.GetTeamByIdAsync(teamId, profileId, userId);
                if (!teamResult.Success)
                {
                    return BadRequest(GenericResponse<TeamStatsResponse>.CreateError("Không tìm thấy team hoặc bạn không có quyền truy cập"));
                }

                // Get team members count
                var teamMembers = await _teamMemberRepository.GetByTeamIdAsync(teamId);
                var membersCount = teamMembers.Count();

                // Calculate members added this month
                var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var membersAddedThisMonth = teamMembers.Count(m => m.JoinedAt >= firstDayOfMonth);

                // Get team brands
                var brands = await _brandService.GetBrandsByTeamIdAsync(teamId, userId);
                var brandsCount = brands.Count();
                var brandIds = brands.Select(b => b.Id).ToList();

                // Initialize stats
                int totalContents = 0;
                int contentCreatedThisWeek = 0;
                int pendingApprovals = 0;

                if (brandIds.Any())
                {
                    // Get all contents for team brands
                    var allContents = new List<AISAM.Data.Model.Content>();
                    foreach (var brandId in brandIds)
                    {
                        var contents = await _contentRepository.GetByBrandIdAsync(brandId);
                        allContents.AddRange(contents);
                    }

                    totalContents = allContents.Count;

                    // Calculate content created this week
                    var firstDayOfWeek = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek).Date;
                    contentCreatedThisWeek = allContents.Count(c => c.CreatedAt >= firstDayOfWeek);

                    // Count pending approvals (contents with status PendingApproval)
                    pendingApprovals = allContents.Count(c => c.Status == ContentStatusEnum.PendingApproval);
                }

                var stats = new TeamStatsResponse
                {
                    MembersCount = membersCount,
                    MembersAddedThisMonth = membersAddedThisMonth,
                    BrandsCount = brandsCount,
                    TotalContents = totalContents,
                    ContentCreatedThisWeek = contentCreatedThisWeek,
                    PendingApprovals = pendingApprovals
                };

                return Ok(GenericResponse<TeamStatsResponse>.CreateSuccess(stats, "Lấy thống kê team thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized team stats request for team {TeamId}", teamId);
                return Unauthorized(GenericResponse<TeamStatsResponse>.CreateError("Không có quyền truy cập"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading team stats for team {TeamId}", teamId);
                return StatusCode(500, GenericResponse<TeamStatsResponse>.CreateError("Đã xảy ra lỗi khi lấy thống kê team"));
            }
        }
    }

    /// <summary>
    /// Response DTO for team statistics
    /// </summary>
    public class TeamStatsResponse
    {
        public int MembersCount { get; set; }
        public int MembersAddedThisMonth { get; set; }
        public int BrandsCount { get; set; }
        public int TotalContents { get; set; }
        public int ContentCreatedThisWeek { get; set; }
        public int PendingApprovals { get; set; }
    }
}