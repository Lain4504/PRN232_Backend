using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IEnumerable<AuditLog>> GetByTargetAsync(string targetTable, Guid targetId)
        {
            return await _context.AuditLogs
                .Include(al => al.Actor)
                .Where(al => al.TargetTable == targetTable && al.TargetId == targetId)
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetByTargetPagedAsync(
            string targetTable, 
            Guid targetId, 
            int page, 
            int pageSize)
        {
            var query = _context.AuditLogs
                .Include(al => al.Actor)
                .Where(al => al.TargetTable == targetTable && al.TargetId == targetId);

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderByDescending(al => al.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}