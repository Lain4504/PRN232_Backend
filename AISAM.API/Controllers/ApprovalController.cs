using Microsoft.AspNetCore.Mvc;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using Microsoft.AspNetCore.Authorization;
using AISAM.API.Authorization;
using AISAM.Repositories.IRepositories;
using System.Security.Claims;
using AISAM.Common.Dtos;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/approvals")]
    public class ApprovalController : ControllerBase
    {
        private readonly IApprovalService _approvalService;
        private readonly IApprovalRepository _approvalRepository;
        private readonly IContentRepository _contentRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(
            IApprovalService approvalService,
            IApprovalRepository approvalRepository,
            IContentRepository contentRepository,
            IAuthorizationService authorizationService,
            ILogger<ApprovalController> logger)
        {
            _approvalService = approvalService;
            _approvalRepository = approvalRepository;
            _contentRepository = contentRepository;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        /// <summary>
        /// Create new approval request
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ApprovalResponseDto>>> CreateApproval([FromBody] CreateApprovalRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            _logger.LogInformation("CreateApproval request started. UserId: {UserId}, Role: {UserRole}, ContentId: {ContentId}, ApproverId: {ApproverId}", 
                userId, userRole, request.ContentId, request.ApproverId);

            try
            {
                // Get content to check authorization
                var content = await _contentRepository.GetByIdAsync(request.ContentId);
                if (content == null)
                {
                    _logger.LogWarning("CreateApproval failed: Content {ContentId} not found. UserId: {UserId}", 
                        request.ContentId, userId);
                    return NotFound(GenericResponse<ApprovalResponseDto>.CreateError("Không tìm thấy nội dung"));
                }

                _logger.LogInformation("CreateApproval: Found content {ContentId}, BrandId: {BrandId}, BrandOwnerId: {BrandOwnerId}", 
                    content.Id, content.BrandId, content.Brand?.UserId);

                // Check authorization - user must own the brand of the content
                var authResult = await _authorizationService.AuthorizeAsync(User, content, Operations.Create);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning("CreateApproval authorization FAILED. UserId: {UserId}, ContentId: {ContentId}, BrandId: {BrandId}, BrandOwnerId: {BrandOwnerId}", 
                        userId, request.ContentId, content.BrandId, content.Brand?.UserId);
                    return Forbid();
                }

                _logger.LogInformation("CreateApproval authorization SUCCESS. UserId: {UserId}, ContentId: {ContentId}", 
                    userId, request.ContentId);

                var result = await _approvalService.CreateApprovalAsync(request);
                
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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            _logger.LogInformation("GetApproval request started. UserId: {UserId}, Role: {UserRole}, ApprovalId: {ApprovalId}", 
                userId, userRole, id);

            try
            {
                // Get approval with related data for authorization
                var approval = await _approvalRepository.GetByIdAsync(id);
                if (approval == null)
                {
                    _logger.LogWarning("GetApproval failed: Approval {ApprovalId} not found. UserId: {UserId}", 
                        id, userId);
                    return NotFound(GenericResponse<ApprovalResponseDto>.CreateError("Không tìm thấy yêu cầu phê duyệt"));
                }

                _logger.LogInformation("GetApproval: Found approval {ApprovalId}, ContentId: {ContentId}, BrandId: {BrandId}, ApproverId: {ApproverId}", 
                    approval.Id, approval.ContentId, approval.Content?.BrandId, approval.ApproverId);

                // Check authorization
                var authResult = await _authorizationService.AuthorizeAsync(User, approval, Operations.Read);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning("GetApproval authorization FAILED. UserId: {UserId}, ApprovalId: {ApprovalId}, ContentId: {ContentId}, BrandOwnerId: {BrandOwnerId}, ApproverId: {ApproverId}", 
                        userId, id, approval.ContentId, approval.Content?.Brand?.UserId, approval.ApproverId);
                    return Forbid();
                }

                _logger.LogInformation("GetApproval authorization SUCCESS. UserId: {UserId}, ApprovalId: {ApprovalId}", 
                    userId, id);

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
                // Get approval with related data for authorization
                var approval = await _approvalRepository.GetByIdAsync(id);
                if (approval == null)
                {
                    return NotFound(GenericResponse<ApprovalResponseDto>.CreateError("Không tìm thấy yêu cầu phê duyệt"));
                }

                // Check authorization
                var authResult = await _authorizationService.AuthorizeAsync(User, approval, Operations.Update);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                var result = await _approvalService.UpdateApprovalAsync(id, request);
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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            _logger.LogInformation("ApproveContent request started. UserId: {UserId}, Role: {UserRole}, ApprovalId: {ApprovalId}", 
                userId, userRole, id);

            try
            {
                // Get approval with related data for authorization
                var approval = await _approvalRepository.GetByIdAsync(id);
                if (approval == null)
                {
                    _logger.LogWarning("ApproveContent failed: Approval {ApprovalId} not found. UserId: {UserId}", 
                        id, userId);
                    return NotFound(GenericResponse<ApprovalResponseDto>.CreateError("Không tìm thấy yêu cầu phê duyệt"));
                }

                _logger.LogInformation("ApproveContent: Found approval {ApprovalId}, ContentId: {ContentId}, BrandId: {BrandId}, BrandOwnerId: {BrandOwnerId}, ApproverId: {ApproverId}", 
                    approval.Id, approval.ContentId, approval.Content?.Brand?.Id, approval.Content?.Brand?.UserId, approval.ApproverId);

                // Check authorization
                var authResult = await _authorizationService.AuthorizeAsync(User, approval, Operations.Approve);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning("ApproveContent authorization FAILED. UserId: {UserId}, ApprovalId: {ApprovalId}, BrandOwnerId: {BrandOwnerId}, ApproverId: {ApproverId}", 
                        userId, id, approval.Content?.Brand?.UserId, approval.ApproverId);
                    return Forbid();
                }

                _logger.LogInformation("ApproveContent authorization SUCCESS. UserId: {UserId}, ApprovalId: {ApprovalId}", 
                    userId, id);

                var result = await _approvalService.ApproveAsync(id, notes);
                
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
                // Get approval with related data for authorization
                var approval = await _approvalRepository.GetByIdAsync(id);
                if (approval == null)
                {
                    return NotFound(GenericResponse<ApprovalResponseDto>.CreateError("Không tìm thấy yêu cầu phê duyệt"));
                }

                // Check authorization
                var authResult = await _authorizationService.AuthorizeAsync(User, approval, Operations.Reject);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                var result = await _approvalService.RejectAsync(id, notes);
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
                // Get current user info
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (!Guid.TryParse(userIdClaim, out var userId) || 
                    !Enum.TryParse<UserRoleEnum>(roleClaim, out var userRole))
                {
                    return Unauthorized();
                }

                var request = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                // For regular users, filter by their brands or assigned approvals
                if (userRole == UserRoleEnum.User)
                {
                    // If no specific filters provided, show approvals for user's brands or assigned to them
                    if (contentId == null && approverId == null)
                    {
                        // This will be handled in the service layer to filter by user's brands
                        // For now, we'll pass the userId to filter appropriately
                    }
                }

                var result = await _approvalService.GetPagedApprovalsAsync(request, status, contentId, approverId, onlyDeleted);
                
                // Filter results based on authorization for regular users
                if (userRole == UserRoleEnum.User)
                {
                    var authorizedApprovals = new List<ApprovalResponseDto>();
                    
                    foreach (var approval in result.Data)
                    {
                        // Get the actual approval entity for authorization check
                        var approvalEntity = await _approvalRepository.GetByIdAsync(approval.Id);
                        if (approvalEntity != null)
                        {
                            var authResult = await _authorizationService.AuthorizeAsync(User, approvalEntity, Operations.Read);
                            if (authResult.Succeeded)
                            {
                                authorizedApprovals.Add(approval);
                            }
                        }
                    }
                    
                    result.Data = authorizedApprovals;
                    result.TotalCount = authorizedApprovals.Count;
                }

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
        /// Hard delete approval permanently
        /// </summary>
        [HttpDelete("{id}/permanent")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<bool>>> HardDeleteApproval(Guid id)
        {
            try
            {
                var result = await _approvalService.HardDeleteAsync(id);
                if (!result)
                {
                    return NotFound(GenericResponse<bool>.CreateError("Không tìm thấy yêu cầu phê duyệt"));
                }

                return Ok(GenericResponse<bool>.CreateSuccess(result, "Xóa vĩnh viễn yêu cầu phê duyệt thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting approval {ApprovalId}", id);
                return StatusCode(500, GenericResponse<bool>.CreateError("Lỗi hệ thống khi xóa vĩnh viễn yêu cầu phê duyệt"));
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