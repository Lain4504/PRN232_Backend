using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class ContentRepository : IContentRepository
    {
        private readonly AisamContext _context;

        public ContentRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Content?> GetByIdAsync(Guid id)
        {
            return await _context.Contents
                .Include(c => c.Brand)
                .Include(c => c.Product)
                .Include(c => c.Posts)
                .Include(c => c.ContentCalendars)
                .Include(c => c.Approvals)
                .Include(c => c.AdCreatives)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<Content?> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _context.Contents
                .Include(c => c.Brand)
                .Include(c => c.Product)
                .Include(c => c.Posts)
                .Include(c => c.ContentCalendars)
                .Include(c => c.Approvals)
                .Include(c => c.AdCreatives)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Content>> GetByBrandIdAsync(Guid brandId)
        {
            return await _context.Contents
                .Include(c => c.Brand)
                .Include(c => c.Product)
                .Include(c => c.Posts)
                .Where(c => c.BrandId == brandId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        

        public async Task<(IEnumerable<Content> Items, int TotalCount)> GetByBrandIdPagedAsync(
            Guid brandId,
            int page,
            int pageSize,
            string? searchTerm,
            string? sortBy,
            bool sortDescending,
            AdTypeEnum? adType,
            bool onlyDeleted,
            ContentStatusEnum? status)
        {
            var query = _context.Contents
                .Include(c => c.Brand)
                .Include(c => c.Product)
                .Include(c => c.Posts)
                .Where(c => c.BrandId == brandId);

            query = onlyDeleted ? query.Where(c => c.IsDeleted) : query.Where(c => !c.IsDeleted);

            if (adType.HasValue)
            {
                query = query.Where(c => c.AdType == adType.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(c =>
                    (c.Title != null && c.Title.ToLower().Contains(term)) ||
                    (c.TextContent != null && c.TextContent.ToLower().Contains(term))
                );
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                bool desc = sortDescending;
                switch (sortBy.Trim().ToLower())
                {
                    case "title":
                        query = desc ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title);
                        break;
                    case "updatedat":
                        query = desc ? query.OrderByDescending(c => c.UpdatedAt) : query.OrderBy(c => c.UpdatedAt);
                        break;
                    default:
                        query = desc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(c => c.CreatedAt);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Content> CreateAsync(Content content)
        {
            content.CreatedAt = DateTime.UtcNow;
            content.UpdatedAt = DateTime.UtcNow;
            
            _context.Contents.Add(content);
            await _context.SaveChangesAsync();
            return content;
        }

        public async Task UpdateAsync(Content content)
        {
            content.UpdatedAt = DateTime.UtcNow;
            _context.Contents.Update(content);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var content = await _context.Contents
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                
            if (content != null)
            {
                content.IsDeleted = true;
                content.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RestoreAsync(Guid id)
        {
            var content = await _context.Contents
                .FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);

            if (content != null)
            {
                content.IsDeleted = false;
                content.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task HardDeleteAsync(Guid id)
        {
            var content = await _context.Contents
                .FirstOrDefaultAsync(c => c.Id == id);

            if (content != null)
            {
                _context.Contents.Remove(content);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Contents
                .AnyAsync(c => c.Id == id && !c.IsDeleted);
        }
    }
}
