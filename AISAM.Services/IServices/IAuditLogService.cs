using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IAuditLogService
    {
        Task LogContentActionAsync(Guid contentId, Guid actorId, string actionType, object? oldValues = null, object? newValues = null, string? notes = null);
        Task<PagedResult<AuditLogResponseDto>> GetContentAuditLogsAsync(Guid contentId, Guid userId, PaginationRequest request);
    }
}