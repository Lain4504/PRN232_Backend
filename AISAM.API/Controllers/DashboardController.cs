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
        }

        [HttpGet("stats")]
        public async Task<ActionResult<GenericResponse<DashboardStatsResponse>>> GetStats()
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

                // Teams count for current user
                var userTeams = await _teamService.GetUserTeamsAsync(userId);
                var teamsCount = userTeams?.Data?.Count() ?? 0;

                // Brands count for current profile (use paged endpoint's total count for efficiency)
                var pageRequest = new PaginationRequest
                {
                    Page = 1,
                    PageSize = 1
                };
                var brandsPaged = await _brandService.GetPagedByProfileIdAsync(profileId, pageRequest);
                var brandsCount = brandsPaged?.TotalCount ?? 0;

                // Pending approvals for current user
                var pendingApprovals = await _approvalService.GetPendingCountAsync(userId);

                // Total contents (not available via service by profile/team in current API surface) -> 0 for now
                var stats = new DashboardStatsResponse
                {
                    TeamsCount = teamsCount,
                    BrandsCount = brandsCount,
                    PendingApprovalsCount = pendingApprovals,
                    TotalContents = 0,
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


