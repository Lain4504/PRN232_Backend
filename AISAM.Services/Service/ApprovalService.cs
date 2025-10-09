using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;
using AISAM.Services.Helper;

namespace AISAM.Services.Service
{
    public class ApprovalService : IApprovalService
    {
        private readonly IApprovalRepository _approvalRepository;
        private readonly IContentRepository _contentRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(
            IApprovalRepository approvalRepository,
            IContentRepository contentRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            ITeamMemberRepository teamMemberRepository,
            ISubscriptionRepository subscriptionRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<ApprovalService> logger)
        {
            _approvalRepository = approvalRepository;
            _contentRepository = contentRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _teamMemberRepository = teamMemberRepository;
            _subscriptionRepository = subscriptionRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _logger = logger;
        }

        public async Task<ApprovalResponseDto> SubmitForApprovalAsync(Guid contentId, Guid actorUserId)
        {
            var content = await _contentRepository.GetByIdAsync(contentId) ?? throw new ArgumentException("Content not found");

            if (content.Status != ContentStatusEnum.Draft)
            {
                throw new ArgumentException("Content must be in DRAFT to submit");
            }

            // Check if user has permission to submit for approval
            var canSubmit = await CanUserPerformActionAsync(actorUserId, "SUBMIT_FOR_APPROVAL");
            if (!canSubmit)
            {
                throw new UnauthorizedAccessException("You are not allowed to submit content for approval");
            }

            var brandOwnerId = content.Brand.UserId;

            // quota check based on brand owner
            // var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(brandOwnerId);
            // if (subscription != null)
            // {
            //     var used = await _subscriptionRepository.CountApprovedOrPublishedThisMonthAsync(brandOwnerId);
            //     if (used >= subscription.QuotaPostsPerMonth)
            //     {
            //         throw new UnauthorizedAccessException("Quota exceeded for this month");
            //     }
            // }

            var actor = await _userRepository.GetByIdAsync(actorUserId) ?? throw new ArgumentException("User not found");

            // Permissions & approvers resolve
            var approverIds = new List<Guid>();

            if (actor.Role == UserRoleEnum.Admin)
            {
                // admin auto-approve path
                content.Status = ContentStatusEnum.Approved;
                await _contentRepository.UpdateAsync(content);

                var approval = await _approvalRepository.CreateAsync(new Approval
                {
                    ContentId = content.Id,
                    ApproverId = actorUserId,
                    Status = ContentStatusEnum.Approved,
                    Notes = "Auto-approved by admin",
                    ApprovedAt = DateTime.UtcNow
                });

                // await _notificationRepository.CreateAsync(new Notification
                // {
                //     UserId = brandOwnerId,
                //     Title = "Content approved",
                //     Message = $"Your content '{content.Title}' was auto-approved by admin",
                //     Type = NotificationTypeEnum.SystemUpdate,
                //     TargetId = content.Id,
                //     TargetType = "content"
                // });

                _logger.LogInformation("Admin {AdminId} auto-approved content {ContentId}", actorUserId, content.Id);
                return MapToResponseDto(approval);
            }

            if (actorUserId == brandOwnerId)
            {
                // owner self-approval allowed as auto-approval (simple user)
                content.Status = ContentStatusEnum.Approved;
                await _contentRepository.UpdateAsync(content);

                var approval = await _approvalRepository.CreateAsync(new Approval
                {
                    ContentId = content.Id,
                    ApproverId = actorUserId,
                    Status = ContentStatusEnum.Approved,
                    Notes = "Auto-approved by owner",
                    ApprovedAt = DateTime.UtcNow
                });

                // await _notificationRepository.CreateAsync(new Notification
                // {
                //     UserId = brandOwnerId,
                //     Title = "Content approved",
                //     Message = $"Your content '{content.Title}' was auto-approved",
                //     Type = NotificationTypeEnum.SystemUpdate,
                //     TargetId = content.Id,
                //     TargetType = "content"
                // });

                _logger.LogInformation("Owner {OwnerId} auto-approved content {ContentId}", actorUserId, content.Id);
                return MapToResponseDto(approval);
            }

            // Vendor team multi-approver: approvers are team members with APPROVE_CONTENT permission
            var teamMembers = await _teamMemberRepository.GetByVendorIdAsync(actorUserId);
            var canApproveMembers = teamMembers
                .Where(tm => _rolePermissionConfig.HasCustomPermission(tm.Permissions, "APPROVE_CONTENT"))
                .Select(tm => tm.UserId)
                .Distinct()
                .ToList();

            if (canApproveMembers.Count == 0)
            {
                // fallback: assign to brand owner
                approverIds.Add(brandOwnerId);
            }
            else
            {
                approverIds.AddRange(canApproveMembers);
            }

            // Create approvals pending and update content status
            foreach (var approverId in approverIds)
            {
                await _approvalRepository.CreateAsync(new Approval
                {
                    ContentId = content.Id,
                    ApproverId = approverId,
                    Status = ContentStatusEnum.PendingApproval
                });

                // await _notificationRepository.CreateAsync(new Notification
                // {
                //     UserId = approverId,
                //     Title = "Approval needed",
                //     Message = $"Please review content '{content.Title}'",
                //     Type = NotificationTypeEnum.ApprovalNeeded,
                //     TargetId = content.Id,
                //     TargetType = "content"
                // });
            }

            content.Status = ContentStatusEnum.PendingApproval;
            await _contentRepository.UpdateAsync(content);

            _logger.LogInformation("User {UserId} submitted content {ContentId} for approval to {Count} approvers", actorUserId, content.Id, approverIds.Count);
            // Return first approval created for simplicity
            var first = await _approvalRepository.GetByContentIdAsync(content.Id);
            return MapToResponseDto(first.First());
        }

        public async Task<PagedResult<ApprovalResponseDto>> GetPendingApprovalsAsync(PaginationRequest request, Guid actorUserId)
        {
            // Check if user has permission to view approvals
            var canView = await CanUserPerformActionAsync(actorUserId, "VIEW_APPROVALS");
            if (!canView)
            {
                throw new UnauthorizedAccessException("You are not allowed to view approvals");
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
            var (items, total) = await _approvalRepository.GetPagedAsync(page, pageSize, request.SearchTerm, request.SortBy, request.SortDescending, ContentStatusEnum.PendingApproval, null, actorUserId, false);
            return new PagedResult<ApprovalResponseDto>
            {
                Data = items.Select(MapToResponseDto).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
        public async Task<ApprovalResponseDto> CreateApprovalAsync(CreateApprovalRequest request, Guid actorUserId)
        {
            // Check if user has permission to manage approvals
            var canManage = await CanUserPerformActionAsync(actorUserId, "MANAGE_APPROVALS");
            if (!canManage)
            {
                throw new UnauthorizedAccessException("You are not allowed to create approvals");
            }

            // Validate content exists
            var content = await _contentRepository.GetByIdAsync(request.ContentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            if (content.Status != ContentStatusEnum.Draft)
            {
                throw new ArgumentException("Content status must be DRAFT to create approval");
            }

            // Validate approver exists and has approval permission
            var approver = await _userRepository.GetByIdAsync(request.ApproverId);
            if (approver == null)
            {
                throw new ArgumentException("Approver not found");
            }

            var canApproverApprove = await CanUserPerformActionAsync(request.ApproverId, "APPROVE_CONTENT");
            if (!canApproverApprove)
            {
                throw new ArgumentException("Selected approver does not have permission to approve content");
            }

            // Create approval entity
            var approval = new Approval
            {
                ContentId = request.ContentId,
                ApproverId = request.ApproverId,
                Status = ContentStatusEnum.PendingApproval,
                Notes = request.Notes
            };

            var createdApproval = await _approvalRepository.CreateAsync(approval);
            
            // Update content status to pending approval
            content.Status = ContentStatusEnum.PendingApproval;
            await _contentRepository.UpdateAsync(content);

            _logger.LogInformation("Created approval {ApprovalId} for content {ContentId}", 
                createdApproval.Id, request.ContentId);

            return MapToResponseDto(createdApproval);
        }

        public async Task<ApprovalResponseDto> UpdateApprovalAsync(Guid approvalId, UpdateApprovalRequest request, Guid actorUserId)
        {
            var approval = await _approvalRepository.GetByIdAsync(approvalId);
            if (approval == null)
            {
                throw new ArgumentException("Approval not found");
            }

            // Check if user has permission to update this approval
            var canUpdate = approval.ApproverId == actorUserId || await CanUserPerformActionAsync(actorUserId, "MANAGE_APPROVALS");
            if (!canUpdate)
            {
                throw new UnauthorizedAccessException("You are not allowed to update this approval");
            }

            approval.Status = request.Status;
            approval.Notes = request.Notes;
            
            if (request.Status == ContentStatusEnum.Approved || request.Status == ContentStatusEnum.Rejected)
            {
                approval.ApprovedAt = DateTime.UtcNow;
            }

            await _approvalRepository.UpdateAsync(approval);

            // Update content status
            var content = await _contentRepository.GetByIdAsync(approval.ContentId);
            if (content != null)
            {
                content.Status = request.Status;
                await _contentRepository.UpdateAsync(content);
            }

            _logger.LogInformation("Updated approval {ApprovalId} status to {Status}", 
                approvalId, request.Status);

            return MapToResponseDto(approval);
        }

        public async Task<ApprovalResponseDto?> GetApprovalByIdAsync(Guid approvalId)
        {
            var approval = await _approvalRepository.GetByIdAsync(approvalId);
            return approval != null ? MapToResponseDto(approval) : null;
        }

        public async Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByContentIdAsync(Guid contentId)
        {
            var approvals = await _approvalRepository.GetByContentIdAsync(contentId);
            return approvals.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByApproverIdAsync(Guid approverId)
        {
            var approvals = await _approvalRepository.GetByApproverIdAsync(approverId);
            return approvals.Select(MapToResponseDto);
        }

        public async Task<PagedResult<ApprovalResponseDto>> GetPagedApprovalsAsync(
            PaginationRequest request,
            ContentStatusEnum? status = null,
            Guid? contentId = null,
            Guid? approverId = null,
            bool onlyDeleted = false,
            Guid? filterByUserId = null)
        {
            var (items, totalCount) = await _approvalRepository.GetPagedAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                status,
                contentId,
                approverId,
                onlyDeleted);

            return new PagedResult<ApprovalResponseDto>
            {
                Data = items.Select(MapToResponseDto).ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<ApprovalResponseDto> ApproveAsync(Guid approvalId, Guid actorUserId, string? notes = null)
        {
            // validate permissions: actor must be the approver or have APPROVE_CONTENT permission
            var approval = await _approvalRepository.GetByIdAsync(approvalId) ?? throw new ArgumentException("Approval not found");
            
            // Check if user is the assigned approver or has permission to approve
            var canApprove = approval.ApproverId == actorUserId || await CanUserPerformActionAsync(actorUserId, "APPROVE_CONTENT");
            if (!canApprove)
            {
                throw new UnauthorizedAccessException("You are not allowed to approve this item");
            }

            var updateRequest = new UpdateApprovalRequest
            {
                Status = ContentStatusEnum.Approved,
                Notes = notes
            };

            return await UpdateApprovalAsync(approvalId, updateRequest, actorUserId);
        }

        public async Task<ApprovalResponseDto> RejectAsync(Guid approvalId, Guid actorUserId, string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                throw new ArgumentException("Notes are required to reject");
            }

            // validate permissions: actor must be the approver or have REJECT_CONTENT permission
            var approval = await _approvalRepository.GetByIdAsync(approvalId) ?? throw new ArgumentException("Approval not found");
            
            // Check if user is the assigned approver or has permission to reject
            var canReject = approval.ApproverId == actorUserId || await CanUserPerformActionAsync(actorUserId, "REJECT_CONTENT");
            if (!canReject)
            {
                throw new UnauthorizedAccessException("You are not allowed to reject this item");
            }

            var updateRequest = new UpdateApprovalRequest
            {
                Status = ContentStatusEnum.Rejected,
                Notes = notes
            };

            return await UpdateApprovalAsync(approvalId, updateRequest, actorUserId);
        }

        public async Task<bool> SoftDeleteAsync(Guid approvalId)
        {
            try
            {
                await _approvalRepository.DeleteAsync(approvalId);
                _logger.LogInformation("Soft deleted approval {ApprovalId}", approvalId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting approval {ApprovalId}", approvalId);
                return false;
            }
        }

        public async Task<bool> RestoreAsync(Guid approvalId)
        {
            try
            {
                await _approvalRepository.RestoreAsync(approvalId);
                _logger.LogInformation("Restored approval {ApprovalId}", approvalId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring approval {ApprovalId}", approvalId);
                return false;
            }
        }

        public async Task<bool> HasPendingApprovalAsync(Guid contentId)
        {
            return await _approvalRepository.HasPendingApprovalAsync(contentId);
        }

        private static ApprovalResponseDto MapToResponseDto(Approval approval)
        {
            return new ApprovalResponseDto
            {
                Id = approval.Id,
                ContentId = approval.ContentId,
                ApproverId = approval.ApproverId,
                Status = approval.Status.ToString(),
                Notes = approval.Notes,
                ApprovedAt = approval.ApprovedAt,
                CreatedAt = approval.CreatedAt,
                Content = approval.Content != null ? new ContentResponseDto
                {
                    Id = approval.Content.Id,
                    BrandId = approval.Content.BrandId,
                    ProductId = approval.Content.ProductId,
                    AdType = approval.Content.AdType.ToString(),
                    Title = approval.Content.Title,
                    TextContent = approval.Content.TextContent,
                    ImageUrl = approval.Content.ImageUrl,
                    VideoUrl = approval.Content.VideoUrl,
                    StyleDescription = approval.Content.StyleDescription,
                    ContextDescription = approval.Content.ContextDescription,
                    RepresentativeCharacter = approval.Content.RepresentativeCharacter,
                    Status = approval.Content.Status.ToString(),
                    CreatedAt = approval.Content.CreatedAt,
                    UpdatedAt = approval.Content.UpdatedAt
                } : null,
                Approver = approval.Approver != null ? new UserResponseDto
                {
                    Id = approval.Approver.Id,
                    Email = approval.Approver.Email,
                    CreatedAt = approval.Approver.CreatedAt
                } : null
            };
        }

        /// <summary>
        /// Validate if a user can perform an approval action
        /// </summary>
        private async Task<bool> CanUserPerformActionAsync(Guid userId, string permission)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // Admin users have all permissions
            if (user.Role == UserRoleEnum.Admin)
            {
                return true;
            }

            // Check team member's actual permissions (not role-based)
            var teamMember = await _teamMemberRepository.GetByUserIdAsync(userId);
            if (teamMember != null)
            {
                // Only check the actual permissions assigned to this team member
                return _rolePermissionConfig.HasCustomPermission(teamMember.Permissions, permission);
            }

            return false;
        }
    }
}