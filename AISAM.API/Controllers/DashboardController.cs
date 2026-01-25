using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Services.IServices;
using AISAM.API.Utils;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IBrandService _brandService;
        private readonly IApprovalService _approvalService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ITeamService teamService,
            IBrandService brandService,
            IApprovalService approvalService,
            ILogger<DashboardController> logger)
        {
            _teamService = teamService;
            _brandService = brandService;
            _approvalService = approvalService;
            _logger = logger;
        }

        public class DashboardStatsResponse
        {
            public int TeamsCount { get; set; }
            public int BrandsCount { get; set; }
            public int PendingApprovalsCount { get; set; }
            public int TotalContents { get; set; }
            public int MembersCount { get; set; }
            public int ScheduledPostsCount { get; set; }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<GenericResponse<DashboardStatsResponse>>> GetStats([FromQuery] Guid? teamId = null)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

                int teamsCount = 0;
                int brandsCount = 0;
                int pendingApprovals = 0;
                int totalContents = 0;
                int membersCount = 0;
                int scheduledPosts = 0;

                if (teamId.HasValue)
                {
                    // Scoped by Team
                    var teamResult = await _teamService.GetTeamByIdAsync(teamId.Value, profileId, userId);
                    if (!teamResult.Success)
                    {
                        return BadRequest(GenericResponse<DashboardStatsResponse>.CreateError("Team not found or access denied"));
                    }

                    // Brands in team
                    var brands = await _brandService.GetBrandsByTeamIdAsync(teamId.Value, userId);
                    brandsCount = brands.Count();
                    var brandIds = brands.Select(b => b.Id).ToList();

                    // Contents in team brands
                    foreach (var bId in brandIds)
                    {
                        var contents = await _brandService.GetPagedContentsByBrandIdAsync(bId, new PaginationRequest { PageSize = 1000 });
                        totalContents += contents?.Data?.Count() ?? 0;
                        pendingApprovals += contents?.Data?.Count(c => c.Status == "PendingApproval") ?? 0; // Wait, Status is string now in DTO.
                    }
                    
                    // Members in team
                    var teamMembers = await _teamService.GetTeamMembersAsync(teamId.Value, profileId, userId);
                    membersCount = teamMembers?.Data?.Count() ?? 0;
                }
                else
                {
                    // Profile-wide Stats
                    var userTeams = await _teamService.GetUserTeamsAsync(userId);
                    teamsCount = userTeams?.Data?.Count() ?? 0;

                    var brandsPaged = await _brandService.GetPagedByProfileIdAsync(profileId, userId, new PaginationRequest { PageSize = 1 });
                    brandsCount = brandsPaged?.TotalCount ?? 0;

                    pendingApprovals = await _approvalService.GetPendingCountAsync(userId);
                    
                    // For global total contents, we might need a more efficient way later
                    // For now, let's keep it 0 or count across brands if feasible
                    totalContents = 0; 
                }

                var stats = new DashboardStatsResponse
                {
                    TeamsCount = teamsCount,
                    BrandsCount = brandsCount,
                    PendingApprovalsCount = pendingApprovals,
                    TotalContents = totalContents,
                    MembersCount = membersCount,
                    ScheduledPostsCount = scheduledPosts
                };

                return Ok(GenericResponse<DashboardStatsResponse>.CreateSuccess(stats, "Dashboard stats loaded"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized dashboard stats request");
                return Unauthorized(GenericResponse<DashboardStatsResponse>.CreateError("Không thể xác thực người dùng"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard stats");
                return StatusCode(500, GenericResponse<DashboardStatsResponse>.CreateError("Đã xảy ra lỗi khi lấy thống kê dashboard"));
            }
        }
    }
}


