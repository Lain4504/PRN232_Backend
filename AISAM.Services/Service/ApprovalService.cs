using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using AISAM.Services.Helper;

namespace AISAM.Services.Service
{
    public class ApprovalService : IApprovalService
    {
        private readonly IApprovalRepository _approvalRepository;
        private readonly IContentRepository _contentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(
            IApprovalRepository approvalRepository,
            IContentRepository contentRepository,
            IUserRepository userRepository,
            ITeamMemberRepository teamMemberRepository,
            IBrandRepository brandRepository,
            IProfileRepository profileRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<ApprovalService> logger)
        {
            _approvalRepository = approvalRepository;
            _contentRepository = contentRepository;
            _userRepository = userRepository;
            _teamMemberRepository = teamMemberRepository;
            _brandRepository = brandRepository;
            _profileRepository = profileRepository;
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
            var canSubmit = await CanUserPerformActionAsync(actorUserId, "SUBMIT_FOR_APPROVAL", content.BrandId);
            if (!canSubmit)
            {
                throw new UnauthorizedAccessException("You are not allowed to submit content for approval");
            }

            var brandProfileId = content.Brand.ProfileId;

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
                    ApproverUserId = actorUserId,
                    Status = ContentStatusEnum.Approved,
                    Notes = "Auto-approved by admin",
                    ApprovedAt = DateTime.UtcNow
                });

                // TODO: notify team members that content is approved

                _logger.LogInformation("Admin {AdminId} auto-approved content {ContentId}", actorUserId, content.Id);
                return MapToResponseDto(approval);
            }

            // Auto-approve by brand owner is determined by permission; skip direct userId==profileId comparison
            if (await CanUserPerformActionAsync(actorUserId, "APPROVE_CONTENT", content.BrandId))
            {
                // owner self-approval allowed as auto-approval (simple user)
                content.Status = ContentStatusEnum.Approved;
                await _contentRepository.UpdateAsync(content);

                var approval = await _approvalRepository.CreateAsync(new Approval
                {
                    ContentId = content.Id,
                    ApproverUserId = actorUserId,
                    Status = ContentStatusEnum.Approved,
                    Notes = "Auto-approved by owner",
                    ApprovedAt = DateTime.UtcNow
                });

                // TODO: notify team members that content is approved

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
                approverIds.Add(actorUserId);
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
                    ApproverUserId = approverId,
                    Status = ContentStatusEnum.PendingApproval
                });

                // TODO: notify team members that content is approved
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
            var canView = await CanUserPerformActionAsync(actorUserId, "VIEW_APPROVALS", null);
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
            // Validate content exists
            var content = await _contentRepository.GetByIdAsync(request.ContentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Check if user has permission to manage approvals
            var canManage = await CanUserPerformActionAsync(actorUserId, "SUBMIT_FOR_APPROVAL", content.BrandId);
            if (!canManage)
            {
                throw new UnauthorizedAccessException("You are not allowed to create approvals");
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

            var canApproverApprove = await CanUserPerformActionAsync(request.ApproverId, "APPROVE_CONTENT", content.BrandId);
            if (!canApproverApprove)
            {
                throw new ArgumentException("Selected approver does not have permission to approve content");
            }

            // Create approval entity
            // Map approver (user) to the brand's profile context, since DB stores ApproverProfileId (FK to profiles)
            var brand = await _brandRepository.GetByIdAsync(content.BrandId) ?? throw new ArgumentException("Brand not found");
            var approval = new Approval
            {
                ContentId = request.ContentId,
                ApproverUserId = request.ApproverId,
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

            // Get content to get brandId for permission check
            var content = await _contentRepository.GetByIdAsync(approval.ContentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Check if user has permission to update this approval
            var canUpdate = await CanUserPerformActionAsync(actorUserId, "APPROVE_CONTENT", content.BrandId);
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
            var contentToUpdate = await _contentRepository.GetByIdAsync(approval.ContentId);
            if (contentToUpdate != null)
            {
                contentToUpdate.Status = request.Status;
                await _contentRepository.UpdateAsync(contentToUpdate);
            }

            _logger.LogInformation("Updated approval {ApprovalId} status to {Status}", 
                approvalId, request.Status);

            return MapToResponseDto(approval);
        }

        public async Task<ApprovalResponseDto?> GetApprovalByIdAsync(Guid approvalId, Guid userId)
        {
            // Check if user has permission to view approvals
            var canView = await CanUserPerformActionAsync(userId, "VIEW_APPROVALS", null);
            if (!canView)
            {
                throw new UnauthorizedAccessException("You are not allowed to view approvals");
            }

            var approval = await _approvalRepository.GetByIdAsync(approvalId);
            return approval != null ? MapToResponseDto(approval) : null;
        }

        public async Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByContentIdAsync(Guid contentId, Guid userId)
        {
            // Get content to get brandId for permission check
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Check if user has permission to view approvals
            var canView = await CanUserPerformActionAsync(userId, "VIEW_APPROVALS", content.BrandId);
            if (!canView)
            {
                throw new UnauthorizedAccessException("You are not allowed to view approvals");
            }

            var approvals = await _approvalRepository.GetByContentIdAsync(contentId);
            return approvals.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ApprovalResponseDto>> GetApprovalsByApproverIdAsync(Guid approverId, Guid userId)
        {
            // Check if user has permission to view approvals or is the approver themselves
            var canView = await CanUserPerformActionAsync(userId, "VIEW_APPROVALS", null);
            var isApprover = userId == approverId;
            
            if (!canView && !isApprover)
            {
                throw new UnauthorizedAccessException("You are not allowed to view approvals");
            }

            var approvals = await _approvalRepository.GetByApproverUserIdAsync(approverId);
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
            // Check if user has permission to view approvals
            if (filterByUserId.HasValue)
            {
                var canView = await CanUserPerformActionAsync(filterByUserId.Value, "VIEW_APPROVALS", null);
                if (!canView)
                {
                    throw new UnauthorizedAccessException("You are not allowed to view approvals");
                }
            }

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
            
            // Get content to get brandId for permission check
            var content = await _contentRepository.GetByIdAsync(approval.ContentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Check if user is the assigned approver or has permission to approve
            var canApprove = await CanUserPerformActionAsync(actorUserId, "APPROVE_CONTENT", content.BrandId);
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
            
            // Get content to get brandId for permission check
            var content = await _contentRepository.GetByIdAsync(approval.ContentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Check if user is the assigned approver or has permission to reject
            var canReject = await CanUserPerformActionAsync(actorUserId, "REJECT_CONTENT", content.BrandId);
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

        public async Task<bool> SoftDeleteAsync(Guid approvalId, Guid userId)
        {
            try
            {
                // Check if user has permission to manage approvals (Admin or APPROVE_CONTENT)
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new UnauthorizedAccessException("User not found");
                }

                var canManage = user.Role == UserRoleEnum.Admin || await CanUserPerformActionAsync(userId, "APPROVE_CONTENT", null);
                if (!canManage)
                {
                    throw new UnauthorizedAccessException("You are not allowed to delete approvals");
                }

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

        public async Task<bool> RestoreAsync(Guid approvalId, Guid userId)
        {
            try
            {
                // Check if user has permission to manage approvals (Admin or APPROVE_CONTENT)
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new UnauthorizedAccessException("User not found");
                }

                var canManage = user.Role == UserRoleEnum.Admin || await CanUserPerformActionAsync(userId, "APPROVE_CONTENT", null);
                if (!canManage)
                {
                    throw new UnauthorizedAccessException("You are not allowed to restore approvals");
                }

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

        public async Task<int> GetPendingCountAsync(Guid approverId)
        {
            return await _approvalRepository.GetPendingCountAsync(approverId);
        }

        private static ApprovalResponseDto MapToResponseDto(Approval approval)
        {
            return new ApprovalResponseDto
            {
                Id = approval.Id,
                ContentId = approval.ContentId,
                ApproverProfileId = approval.ApproverProfileId,
                ApproverUserId = approval.ApproverUserId,
                Status = approval.Status.ToString(),
                Notes = approval.Notes,
                ApprovedAt = approval.ApprovedAt,
                CreatedAt = approval.CreatedAt,
                // Flattened convenience properties
                ContentTitle = approval.Content?.Title,
                BrandName = approval.Content?.Brand?.Name,
                BrandId = approval.Content?.BrandId,
                ApproverEmail = approval.ApproverUser?.Email ?? approval.ApproverProfile?.User?.Email,
                ApproverName = approval.ApproverProfile?.Name,
                // Navigation properties (kept for backward compatibility)
                Content = approval.Content != null ? new ContentResponseDto
                {
                    Id = approval.Content.Id,
                    ProfileId = approval.Content.ProfileId,
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
                ApproverProfile = approval.ApproverProfile != null ? new ProfileResponseDto
                {
                    Id = approval.ApproverProfile.Id,
                    Name = approval.ApproverProfile.Name,
                    ProfileType = approval.ApproverProfile.ProfileType,
                    CompanyName = approval.ApproverProfile.CompanyName,
                    Bio = approval.ApproverProfile.Bio,
                    AvatarUrl = approval.ApproverProfile.AvatarUrl,
                    CreatedAt = approval.ApproverProfile.CreatedAt
                } : null
            };
        }

        /// <summary>
        /// Validate if a user can perform an approval action
        /// </summary>
        private async Task<bool> CanUserPerformActionAsync(Guid userId, string permission, Guid? brandId = null)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // If brandId is provided, check if user is brand owner or team member
            if (brandId.HasValue)
            {
                var brand = await _brandRepository.GetByIdAsync(brandId.Value);
                if (brand == null) return false;

                // Check direct ownership through user's profiles
                var userProfiles = await _profileRepository.GetByUserIdAsync(userId);
                if (userProfiles?.Any(p => p.Id == brand.ProfileId) == true)
                {
                    return true; // User owns this brand directly
                }

                // If brand's profile is Free type, only owner can access
                var brandProfile = await _profileRepository.GetByIdAsync(brand.ProfileId);
                if (brandProfile?.ProfileType == ProfileTypeEnum.Free)
                {
                    return false; // Free profiles don't have team features
                }

                // For Basic/Pro profiles: check team member access
                var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brandId.Value);
                if (teamMember == null) return false;

                return _rolePermissionConfig.HasCustomPermission(teamMember.Permissions, permission);
            }

            // Fallback: check if user has permission in any team membership
            var userTeamMembers = await _teamMemberRepository.GetByUserIdWithBrandsAsync(userId);
            if (userTeamMembers != null && userTeamMembers.Any())
            {
                return userTeamMembers.Any(tm => _rolePermissionConfig.HasCustomPermission(tm.Permissions, permission));
            }

            return false;
        }
    }
}