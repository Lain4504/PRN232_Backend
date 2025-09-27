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

        public async Task<IEnumerable<Content>> GetByUserIdAsync(Guid userId)
        {
            // Get contents through brand relationship
            return await _context.Contents
                .Include(c => c.Brand)
                .Include(c => c.Product)
                .Include(c => c.Posts)
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
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

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Contents
                .AnyAsync(c => c.Id == id && !c.IsDeleted);
        }
    }
}
