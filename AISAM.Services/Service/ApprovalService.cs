using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class ApprovalService : IApprovalService
    {
        private readonly IApprovalRepository _approvalRepository;
        private readonly IContentRepository _contentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(
            IApprovalRepository approvalRepository,
            IContentRepository contentRepository,
            IUserRepository userRepository,
            ILogger<ApprovalService> logger)
        {
            _approvalRepository = approvalRepository;
            _contentRepository = contentRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<ApprovalResponseDto> CreateApprovalAsync(CreateApprovalRequest request)
        {
            // Validate content exists
            var content = await _contentRepository.GetByIdAsync(request.ContentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Validate approver exists
            var approver = await _userRepository.GetByIdAsync(request.ApproverId);
            if (approver == null)
            {
                throw new ArgumentException("Approver not found");
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

        public async Task<ApprovalResponseDto> UpdateApprovalAsync(Guid approvalId, UpdateApprovalRequest request)
        {
            var approval = await _approvalRepository.GetByIdAsync(approvalId);
            if (approval == null)
            {
                throw new ArgumentException("Approval not found");
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

        public async Task<ApprovalResponseDto> ApproveAsync(Guid approvalId, string? notes = null)
        {
            var updateRequest = new UpdateApprovalRequest
            {
                Status = ContentStatusEnum.Approved,
                Notes = notes
            };

            return await UpdateApprovalAsync(approvalId, updateRequest);
        }

        public async Task<ApprovalResponseDto> RejectAsync(Guid approvalId, string? notes = null)
        {
            var updateRequest = new UpdateApprovalRequest
            {
                Status = ContentStatusEnum.Rejected,
                Notes = notes
            };

            return await UpdateApprovalAsync(approvalId, updateRequest);
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

        public async Task<bool> HardDeleteAsync(Guid approvalId)
        {
            try
            {
                await _approvalRepository.HardDeleteAsync(approvalId);
                _logger.LogInformation("Hard deleted approval {ApprovalId}", approvalId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting approval {ApprovalId}", approvalId);
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
    }
}