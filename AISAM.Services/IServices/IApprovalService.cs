using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;

namespace AISAM.Services.IServices
{
    public interface IApprovalService
    {
        /// <summary>
        /// Create new approval request
        /// </summary>
        Task<ApprovalResponseDto> CreateApprovalAsync(CreateApprovalRequest request);
        
        /// <summary>
        /// Update approval status and notes
        /// </summary>
        Task<ApprovalResponseDto> UpdateApprovalAsync(Guid approvalId, UpdateApprovalRequest request);
        
        /// <summary>
        /// Get approval by ID
        /// </summary>
        Task<ApprovalResponseDto?> GetApprovalByIdAsync(Guid approvalId);
        
        /// <summary>
        /// Get all approvals for a content
        /// </summary>
        Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByContentIdAsync(Guid contentId);
        
        /// <summary>
        /// Get all approvals assigned to an approver
        /// </summary>
        Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByApproverIdAsync(Guid approverId);
        
        /// <summary>
        /// Get paged approvals with filters
        /// </summary>
        Task<PagedResult<ApprovalResponseDto>> GetPagedApprovalsAsync(
            PaginationRequest request,
            ContentStatusEnum? status = null,
            Guid? contentId = null,
            Guid? approverId = null,
            bool onlyDeleted = false,
            Guid? filterByUserId = null);
        
        /// <summary>
        /// Approve content
        /// </summary>
        Task<ApprovalResponseDto> ApproveAsync(Guid approvalId, string? notes = null);
        
        /// <summary>
        /// Reject content
        /// </summary>
        Task<ApprovalResponseDto> RejectAsync(Guid approvalId, string? notes = null);
        
        /// <summary>
        /// Soft delete approval
        /// </summary>
        Task<bool> SoftDeleteAsync(Guid approvalId);
        
        /// <summary>
        /// Restore soft-deleted approval
        /// </summary>
        Task<bool> RestoreAsync(Guid approvalId);
        
        /// <summary>
        /// Hard delete approval permanently
        /// </summary>
        Task<bool> HardDeleteAsync(Guid approvalId);
        
        /// <summary>
        /// Check if content has pending approval
        /// </summary>
        Task<bool> HasPendingApprovalAsync(Guid contentId);
    }
}