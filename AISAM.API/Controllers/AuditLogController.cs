using Microsoft.AspNetCore.Mvc;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using AISAM.API.Utils;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/audit-log")]
    [Authorize]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
        {
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Get audit logs for a specific content (Only TeamLeader can view)
        /// </summary>
        [HttpGet("content/{contentId}")]
        public async Task<ActionResult<GenericResponse<PagedResult<AuditLogResponseDto>>>> GetContentAuditLogs(
            Guid contentId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var request = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _auditLogService.GetContentAuditLogsAsync(contentId, userId, request);

                return Ok(GenericResponse<PagedResult<AuditLogResponseDto>>.CreateSuccess(
                    result,
                    "Lấy nhật ký audit thành công"
                ));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to audit logs for content {ContentId}", contentId);
                return StatusCode(403, GenericResponse<PagedResult<AuditLogResponseDto>>.CreateError(
                    "Chỉ TeamLeader mới có quyền xem nhật ký audit"
                ));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for audit logs of content {ContentId}", contentId);
                return BadRequest(GenericResponse<PagedResult<AuditLogResponseDto>>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs for content {ContentId}", contentId);
                return StatusCode(500, GenericResponse<PagedResult<AuditLogResponseDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy nhật ký audit"
                ));
            }
        }
    }
}
