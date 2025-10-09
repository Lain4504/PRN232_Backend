using AISAM.Common.Dtos;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class PostRepository : IPostRepository
    {
        private readonly AisamContext _context;

        public PostRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Post?> GetByIdAsync(Guid id)
        {
            return await _context.Posts
                .Include(p => p.Content)
                .Include(p => p.Integration)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Post> CreateAsync(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task UpdateAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<Post>> GetPagedAsync(Guid? brandId, Guid? userId, int page, int pageSize, bool includeDeleted = false, ContentStatusEnum? status = null)
        {
            var query = _context.Posts
                .Include(p => p.Content)
                .ThenInclude(c => c.Brand)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(p => !p.IsDeleted);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.Content.BrandId == brandId.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(p => p.Content.Brand.UserId == userId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Post>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Data = items
            };
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return false;
            }
            post.IsDeleted = true;
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

