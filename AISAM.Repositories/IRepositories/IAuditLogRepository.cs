using AISAM.Data.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AISAM.Repositories.IRepositories
{
    public interface IAuditLogRepository
    {
        Task<AuditLog> CreateAsync(AuditLog auditLog);
        Task<List<AuditLog>> GetByTargetAsync(string targetTable, Guid targetId);
    }
}