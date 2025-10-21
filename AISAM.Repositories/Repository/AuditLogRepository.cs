using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using AISAM.Data.Model;

namespace AISAM.Repositories.Repository
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AisamContext _context;

        public AuditLogRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<AuditLog> CreateAsync(AuditLog auditLog)
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            return auditLog;
        }

        public async Task<List<AuditLog>> GetByTargetAsync(string targetTable, Guid targetId)
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.TargetTable == targetTable && a.TargetId == targetId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}