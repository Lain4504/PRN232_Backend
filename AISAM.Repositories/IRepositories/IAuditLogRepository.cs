using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAuditLogRepository
    {
        Task<AuditLog> CreateAsync(AuditLog auditLog);
        Task<IEnumerable<AuditLog>> GetByTargetAsync(string targetTable, Guid targetId);
        Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetByTargetPagedAsync(
            string targetTable, 
            Guid targetId, 
            int page, 
            int pageSize);
    }
}