using Microsoft.AspNetCore.Mvc;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using AISAM.Data.Enumeration;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common.Dtos;
using AISAM.API.Utils;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/approvals")]
    public class ApprovalController : ControllerBase
    {
        private readonly IApprovalService _approvalService;
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(
            IApprovalService approvalService,
            ILogger<ApprovalController> logger)
        {
            _approvalService = approvalService;
            _logger = logger;
        }

        /// <summary>
        /// Get pending approvals assigned to current user
        /// </summary>
        [HttpGet("pending")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<ApprovalResponseDto>>>> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var result = await _approvalService.GetPendingApprovalsAsync(new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize
                }, userId);

                return Ok(GenericResponse<PagedResult<ApprovalResponseDto>>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals");
                return StatusCode(500, GenericResponse<PagedResult<ApprovalResponseDto>>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Create new approval request
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ApprovalResponseDto>>> CreateApproval([FromBody] CreateApprovalRequest request)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            _logger.LogInformation("CreateApproval request started. UserId: {UserId}, ContentId: {ContentId}, ApproverId: {ApproverId}", 
                userId, request.ContentId, request.ApproverId);

            try
            {
                var result = await _approvalService.CreateApprovalAsync(request, userId);
                
                _logger.LogInformation("CreateApproval completed successfully. ApprovalId: {ApprovalId}, UserId: {UserId}", 
                    result.Id, userId);
                
                return Ok(GenericResponse<ApprovalResponseDto>.CreateSuccess(result, "Tạo yêu cầu phê duyệt thành công"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "CreateApproval invalid request. UserId: {UserId}, ContentId: {ContentId}", 
                    userId, request.ContentId);
                return BadRequest(GenericResponse<ApprovalResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateApproval error. UserId: {UserId}, ContentId: {ContentId}", 
                    userId, request.ContentId);
                return StatusCode(500, GenericResponse<ApprovalResponseDto>.CreateError("Lỗi hệ thống khi tạo yêu cầu phê duyệt"));
            }
        }

        /// <summary>
        /// Get approval by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ApprovalResponseDto>>> GetApproval(Guid id)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            _logger.LogInformation("GetApproval request started. UserId: {UserId}, ApprovalId: {ApprovalId}", 
                userId, id);

            try
            {
                var result = await _approvalService.GetApprovalByIdAsync(id);
                return Ok(GenericResponse<ApprovalResponseDto>.CreateSuccess(result!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetApproval error. UserId: {UserId}, ApprovalId: {ApprovalId}", 
                    userId, id);
                return StatusCode(500, GenericResponse<ApprovalResponseDto>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Update approval status and notes
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ApprovalResponseDto>>> UpdateApproval(Guid id, [FromBody] UpdateApprovalRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _approvalService.UpdateApprovalAsync(id, request, userId);
                return Ok(GenericResponse<ApprovalResponseDto>.CreateSuccess(result, "Cập nhật yêu cầu phê duyệt thành công"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for approval update");
                return BadRequest(GenericResponse<ApprovalResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating approval {ApprovalId}", id);
                return StatusCode(500, GenericResponse<ApprovalResponseDto>.CreateError("Lỗi hệ thống khi cập nhật yêu cầu phê duyệt"));
            }
        }

        /// <summary>
        /// Approve content
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ApprovalResponseDto>>> ApproveContent(Guid id, [FromBody] string? notes = null)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            _logger.LogInformation("ApproveContent request started. UserId: {UserId}, ApprovalId: {ApprovalId}", 
                userId, id);

            try
            {
                var result = await _approvalService.ApproveAsync(id, userId, notes);
                
                _logger.LogInformation("ApproveContent completed successfully. ApprovalId: {ApprovalId}, UserId: {UserId}", 
                    id, userId);
                
                return Ok(GenericResponse<ApprovalResponseDto>.CreateSuccess(result, "Phê duyệt nội dung thành công"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "ApproveContent invalid request. UserId: {UserId}, ApprovalId: {ApprovalId}", 
                    userId, id);
                return BadRequest(GenericResponse<ApprovalResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApproveContent error. UserId: {UserId}, ApprovalId: {ApprovalId}", 
                    userId, id);
                return StatusCode(500, GenericResponse<ApprovalResponseDto>.CreateError("Lỗi hệ thống khi phê duyệt nội dung"));
            }
        }

        /// <summary>
        /// Reject content
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ApprovalResponseDto>>> RejectContent(Guid id, [FromBody] string? notes = null)
        {
            try
            {
                var actorId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _approvalService.RejectAsync(id, actorId, notes);
                return Ok(GenericResponse<ApprovalResponseDto>.CreateSuccess(result, "Từ chối nội dung thành công"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for rejection");
                return BadRequest(GenericResponse<ApprovalResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting content {ApprovalId}", id);
                return StatusCode(500, GenericResponse<ApprovalResponseDto>.CreateError("Lỗi hệ thống khi từ chối nội dung"));
            }
        }

        /// <summary>
        /// Get approvals by content ID
        /// </summary>
        [HttpGet("content/{contentId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<IEnumerable<ApprovalResponseDto>>>> GetApprovalsByContent(Guid contentId)
        {
            try
            {
                var result = await _approvalService.GetApprovalsByContentIdAsync(contentId);
                return Ok(GenericResponse<IEnumerable<ApprovalResponseDto>>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approvals for content {ContentId}", contentId);
                return StatusCode(500, GenericResponse<IEnumerable<ApprovalResponseDto>>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Get approvals by approver ID
        /// </summary>
        [HttpGet("approver/{approverId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<IEnumerable<ApprovalResponseDto>>>> GetApprovalsByApprover(Guid approverId)
        {
            try
            {
                var result = await _approvalService.GetApprovalsByApproverIdAsync(approverId);
                return Ok(GenericResponse<IEnumerable<ApprovalResponseDto>>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approvals for approver {ApproverId}", approverId);
                return StatusCode(500, GenericResponse<IEnumerable<ApprovalResponseDto>>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Get paged approvals with filters
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<ApprovalResponseDto>>>> GetPagedApprovals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true,
            [FromQuery] ContentStatusEnum? status = null,
            [FromQuery] Guid? contentId = null,
            [FromQuery] Guid? approverId = null,
            [FromQuery] bool onlyDeleted = false)
        {
            try
            {
                var request = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _approvalService.GetPagedApprovalsAsync(request, status, contentId, approverId, onlyDeleted);
                
                // Authorization filtering removed; service should enforce permissions

                return Ok(GenericResponse<PagedResult<ApprovalResponseDto>>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged approvals");
                return StatusCode(500, GenericResponse<PagedResult<ApprovalResponseDto>>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Soft delete approval
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<bool>>> SoftDeleteApproval(Guid id)
        {
            try
            {
                var result = await _approvalService.SoftDeleteAsync(id);
                if (!result)
                {
                    return NotFound(GenericResponse<bool>.CreateError("Không tìm thấy yêu cầu phê duyệt"));
                }

                return Ok(GenericResponse<bool>.CreateSuccess(result, "Xóa yêu cầu phê duyệt thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting approval {ApprovalId}", id);
                return StatusCode(500, GenericResponse<bool>.CreateError("Lỗi hệ thống khi xóa yêu cầu phê duyệt"));
            }
        }

        /// <summary>
        /// Restore soft-deleted approval
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<bool>>> RestoreApproval(Guid id)
        {
            try
            {
                var result = await _approvalService.RestoreAsync(id);
                if (!result)
                {
                    return NotFound(GenericResponse<bool>.CreateError("Không tìm thấy yêu cầu phê duyệt đã xóa"));
                }

                return Ok(GenericResponse<bool>.CreateSuccess(result, "Khôi phục yêu cầu phê duyệt thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring approval {ApprovalId}", id);
                return StatusCode(500, GenericResponse<bool>.CreateError("Lỗi hệ thống khi khôi phục yêu cầu phê duyệt"));
            }
        }

        /// <summary>
        /// Check if content has pending approval
        /// </summary>
        [HttpGet("content/{contentId}/pending")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<bool>>> HasPendingApproval(Guid contentId)
        {
            try
            {
                var result = await _approvalService.HasPendingApprovalAsync(contentId);
                return Ok(GenericResponse<bool>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pending approval for content {ContentId}", contentId);
                return StatusCode(500, GenericResponse<bool>.CreateError("Lỗi hệ thống"));
            }
        }
    }
}