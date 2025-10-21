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
        Task<ApprovalResponseDto> SubmitForApprovalAsync(Guid contentId, Guid actorProfileId);
        
        /// <summary>
        /// Get pending approvals assigned to a user with pagination
        /// </summary>
        Task<PagedResult<ApprovalResponseDto>> GetPendingApprovalsAsync(PaginationRequest request, Guid actorProfileId);
        /// <summary>
        /// Create new approval request
        /// </summary>
        Task<ApprovalResponseDto> CreateApprovalAsync(CreateApprovalRequest request, Guid actorProfileId);
        
        /// <summary>
        /// Update approval status and notes
        /// </summary>
        Task<ApprovalResponseDto> UpdateApprovalAsync(Guid approvalId, UpdateApprovalRequest request, Guid actorProfileId);
        
        /// <summary>
        /// Get approval by ID
        /// </summary>
        Task<ApprovalResponseDto?> GetApprovalByIdAsync(Guid approvalId, Guid profileId);
        
        /// <summary>
        /// Get all approvals for a content
        /// </summary>
        Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByContentIdAsync(Guid contentId, Guid profileId);
        
        /// <summary>
        /// Get all approvals assigned to an approver
        /// </summary>
        Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByApproverIdAsync(Guid approverId, Guid profileId);
        
        /// <summary>
        /// Get paged approvals with filters
        /// </summary>
        Task<PagedResult<ApprovalResponseDto>> GetPagedApprovalsAsync(
            PaginationRequest request,
            ContentStatusEnum? status = null,
            Guid? contentId = null,
            Guid? approverId = null,
            bool onlyDeleted = false,
            Guid? filterByProfileId = null);
        
        /// <summary>
        /// Approve content
        /// </summary>
        Task<ApprovalResponseDto> ApproveAsync(Guid approvalId, Guid actorProfileId, string? notes = null);
        
        /// <summary>
        /// Reject content
        /// </summary>
        Task<ApprovalResponseDto> RejectAsync(Guid approvalId, Guid actorProfileId, string? notes = null);
        
        /// <summary>
        /// Soft delete approval
        /// </summary>
        Task<bool> SoftDeleteAsync(Guid approvalId, Guid profileId);
        
        /// <summary>
        /// Restore soft-deleted approval
        /// </summary>
        Task<bool> RestoreAsync(Guid approvalId, Guid profileId);
        
        /// <summary>
        /// Check if content has pending approval
        /// </summary>
        Task<bool> HasPendingApprovalAsync(Guid contentId);
    }
}