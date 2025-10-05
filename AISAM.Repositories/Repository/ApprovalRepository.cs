using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class ApprovalRepository : IApprovalRepository
    {
        private readonly AisamContext _context;

        public ApprovalRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Approval?> GetByIdAsync(Guid id)
        {
            return await _context.Approvals
                .Include(a => a.Content)
                    .ThenInclude(c => c.Brand)
                .Include(a => a.Approver)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<Approval?> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _context.Approvals
                .Include(a => a.Content)
                    .ThenInclude(c => c.Brand)
                .Include(a => a.Approver)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Approval>> GetByContentIdAsync(Guid contentId)
        {
            return await _context.Approvals
                .Include(a => a.Content)
                    .ThenInclude(c => c.Brand)
                .Include(a => a.Approver)
                .Where(a => a.ContentId == contentId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Approval>> GetByApproverIdAsync(Guid approverId)
        {
            return await _context.Approvals
                .Include(a => a.Content)
                    .ThenInclude(c => c.Brand)
                .Include(a => a.Approver)
                .Where(a => a.ApproverId == approverId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Approval> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? sortBy,
            bool sortDescending,
            ContentStatusEnum? status,
            Guid? contentId,
            Guid? approverId,
            bool onlyDeleted)
        {
            var query = _context.Approvals
                .Include(a => a.Content)
                    .ThenInclude(c => c.Brand)
                .Include(a => a.Approver)
                .AsQueryable();

            query = onlyDeleted ? query.Where(a => a.IsDeleted) : query.Where(a => !a.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            if (contentId.HasValue)
            {
                query = query.Where(a => a.ContentId == contentId.Value);
            }

            if (approverId.HasValue)
            {
                query = query.Where(a => a.ApproverId == approverId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(a =>
                    (a.Notes != null && a.Notes.ToLower().Contains(term)) ||
                    (a.Content.Title != null && a.Content.Title.ToLower().Contains(term)) ||
                    (a.Approver.Email != null && a.Approver.Email.ToLower().Contains(term))
                );
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                bool desc = sortDescending;
                switch (sortBy.Trim().ToLower())
                {
                    case "status":
                        query = desc ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status);
                        break;
                    case "approvedat":
                        query = desc ? query.OrderByDescending(a => a.ApprovedAt) : query.OrderBy(a => a.ApprovedAt);
                        break;
                    case "approver":
                        query = desc ? query.OrderByDescending(a => a.Approver.Email) : query.OrderBy(a => a.Approver.Email);
                        break;
                    default:
                        query = desc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(a => a.CreatedAt);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Approval> CreateAsync(Approval approval)
        {
            approval.CreatedAt = DateTime.UtcNow;
            
            _context.Approvals.Add(approval);
            await _context.SaveChangesAsync();
            return approval;
        }

        public async Task UpdateAsync(Approval approval)
        {
            _context.Approvals.Update(approval);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var approval = await _context.Approvals
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                
            if (approval != null)
            {
                approval.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task HardDeleteAsync(Guid id)
        {
            var approval = await _context.Approvals
                .FirstOrDefaultAsync(a => a.Id == id);

            if (approval != null)
            {
                _context.Approvals.Remove(approval);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RestoreAsync(Guid id)
        {
            var approval = await _context.Approvals
                .FirstOrDefaultAsync(a => a.Id == id && a.IsDeleted);

            if (approval != null)
            {
                approval.IsDeleted = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Approvals
                .AnyAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<bool> HasPendingApprovalAsync(Guid contentId)
        {
            return await _context.Approvals
                .AnyAsync(a => a.ContentId == contentId && 
                              a.Status == ContentStatusEnum.PendingApproval && 
                              !a.IsDeleted);
        }
    }
}