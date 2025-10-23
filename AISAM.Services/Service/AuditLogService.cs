using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.Helper;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AISAM.Services.Service
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IContentRepository _contentRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            IAuditLogRepository auditLogRepository,
            IContentRepository contentRepository,
            ITeamMemberRepository teamMemberRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<AuditLogService> logger)
        {
            _auditLogRepository = auditLogRepository;
            _contentRepository = contentRepository;
            _teamMemberRepository = teamMemberRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _logger = logger;
        }

        public async Task LogContentActionAsync(Guid contentId, Guid actorId, string actionType, object? oldValues = null, object? newValues = null, string? notes = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    ActorId = actorId,
                    ActionType = actionType,
                    TargetTable = "contents",
                    TargetId = contentId,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow
                };

                await _auditLogRepository.CreateAsync(auditLog);
                _logger.LogInformation("Audit log created for content {ContentId}, action {ActionType} by user {ActorId}", 
                    contentId, actionType, actorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log for content {ContentId}, action {ActionType} by user {ActorId}", 
                    contentId, actionType, actorId);
            }
        }

        public async Task<PagedResult<AuditLogResponseDto>> GetContentAuditLogsAsync(Guid contentId, Guid userId, PaginationRequest request)
        {
            // Check if content exists
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Check if user has TeamLeader role permission to view audit logs
            var hasPermission = await HasTeamLeaderPermissionAsync(userId, content.BrandId);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Only TeamLeader can view audit logs");
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            var (items, totalCount) = await _auditLogRepository.GetByTargetPagedAsync(
                "contents", 
                contentId, 
                page, 
                pageSize);

            var dtos = items.Select(MapToDto).ToList();

            return new PagedResult<AuditLogResponseDto>
            {
                Data = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task<bool> HasTeamLeaderPermissionAsync(Guid userId, Guid brandId)
        {
            // Check if user is team member for this brand
            var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brandId);
            if (teamMember == null)
            {
                return false;
            }

            // Check if team member's role is "TeamLeader"
            return string.Equals(teamMember.Role, "TeamLeader", StringComparison.OrdinalIgnoreCase);
        }

        private static AuditLogResponseDto MapToDto(AuditLog auditLog)
        {
            return new AuditLogResponseDto
            {
                Id = auditLog.Id,
                ActorId = auditLog.ActorId,
                ActorName = auditLog.Actor?.Email ?? "Unknown",
                ActionType = auditLog.ActionType,
                TargetTable = auditLog.TargetTable,
                TargetId = auditLog.TargetId,
                OldValues = auditLog.OldValues,
                NewValues = auditLog.NewValues,
                Notes = auditLog.Notes,
                CreatedAt = auditLog.CreatedAt
            };
        }
    }
}