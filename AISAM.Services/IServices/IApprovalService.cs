using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;

namespace AISAM.Services.IServices
{
    public interface IApprovalService
    {
        // Content approval flow additions
        Task<ApprovalResponseDto> SubmitForApprovalAsync(Guid contentId, Guid actorUserId);
        
        /// <summary>
        /// Get pending approvals assigned to a user with pagination
        /// </summary>
        Task<PagedResult<ApprovalResponseDto>> GetPendingApprovalsAsync(PaginationRequest request, Guid actorUserId);
        /// <summary>
        /// Create new approval request
        /// </summary>
        Task<ApprovalResponseDto> CreateApprovalAsync(CreateApprovalRequest request, Guid actorUserId);
        
        /// <summary>
        /// Update approval status and notes
        /// </summary>
        Task<ApprovalResponseDto> UpdateApprovalAsync(Guid approvalId, UpdateApprovalRequest request, Guid actorUserId);
        
        /// <summary>
        /// Get approval by ID
        /// </summary>
        Task<ApprovalResponseDto?> GetApprovalByIdAsync(Guid approvalId, Guid userId);
        
        /// <summary>
        /// Get all approvals for a content
        /// </summary>
        Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByContentIdAsync(Guid contentId, Guid userId);
        
        /// <summary>
        /// Get all approvals assigned to an approver
        /// </summary>
        Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByApproverIdAsync(Guid approverId, Guid userId);
        
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
        Task<ApprovalResponseDto> ApproveAsync(Guid approvalId, Guid actorUserId, string? notes = null);
        
        /// <summary>
        /// Reject content
        /// </summary>
        Task<ApprovalResponseDto> RejectAsync(Guid approvalId, Guid actorUserId, string? notes = null);
        
        /// <summary>
        /// Soft delete approval
        /// </summary>
        Task<bool> SoftDeleteAsync(Guid approvalId, Guid userId);
        
        /// <summary>
        /// Restore soft-deleted approval
        /// </summary>
        Task<bool> RestoreAsync(Guid approvalId, Guid userId);
        
        /// <summary>
        /// Check if content has pending approval
        /// </summary>
        Task<bool> HasPendingApprovalAsync(Guid contentId);
        
        /// <summary>
        /// Get count of pending approvals for a user
        /// </summary>
        Task<int> GetPendingCountAsync(Guid approverId);
    }
}