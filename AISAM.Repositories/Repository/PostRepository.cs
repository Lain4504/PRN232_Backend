using AISAM.Common.Dtos;
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

        public async Task<PagedResult<Post>> GetPagedByUserIdAsync(Guid userId, PaginationRequest request)
        {
            var query = _context.Posts
                .Include(p => p.Content)
                    .ThenInclude(c => c.Brand)
                .Include(p => p.Integration)
                    .ThenInclude(i => i.SocialAccount)
                .Where(p => p.Content.Brand.UserId == userId && !p.IsDeleted);

            // Search by external post id or content title
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(p => p.ExternalPostId != null && p.ExternalPostId.ToLower().Contains(request.SearchTerm.ToLower()) ||
                                         p.Content.Title != null && p.Content.Title.ToLower().Contains(request.SearchTerm.ToLower()));
            }

            // Sort
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortBy.ToLower() switch
                {
                    "externalpostid" => request.SortDescending
                        ? query.OrderByDescending(p => p.ExternalPostId)
                        : query.OrderBy(p => p.ExternalPostId),
                    "publishedat" => request.SortDescending
                        ? query.OrderByDescending(p => p.PublishedAt)
                        : query.OrderBy(p => p.PublishedAt),
                    "createdat" => request.SortDescending
                        ? query.OrderByDescending(p => p.CreatedAt)
                        : query.OrderBy(p => p.CreatedAt),
                    _ => query.OrderByDescending(p => p.PublishedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(p => p.PublishedAt);
            }

            // Total count
            var totalCount = await query.CountAsync();

            // Paging
            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Post>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<Post> CreateAsync(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return false;
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Post>> GetAllAsync()
        {
            return await _context.Posts
                .Include(p => p.Content)
                    .ThenInclude(c => c.Brand)
                .Include(p => p.Integration)
                    .ThenInclude(i => i.SocialAccount)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();
        }
        public async Task<Post?> GetByIdAsync(Guid id)
        {
            return await _context.Posts
                .Include(p => p.Content)
                    .ThenInclude(c => c.Brand)
                .Include(p => p.Integration)
                    .ThenInclude(i => i.SocialAccount)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return false;
            }

            post.IsDeleted = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return false;
            }

            post.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Post> UpdateAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
            return post;
        }

        // Methods for tracking published posts
        public async Task<IEnumerable<Post>> GetPostsByContentIdAsync(Guid contentId)
        {
            return await _context.Posts
                .Where(p => p.ContentId == contentId && !p.IsDeleted)
                .Include(p => p.Content)
                    .ThenInclude(c => c.Brand)
                .Include(p => p.Integration)
                    .ThenInclude(i => i.SocialAccount)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsByIntegrationIdAsync(Guid integrationId)
        {
            return await _context.Posts
                .Where(p => p.IntegrationId == integrationId && !p.IsDeleted)
                .Include(p => p.Content)
                    .ThenInclude(c => c.Brand)
                .Include(p => p.Integration)
                    .ThenInclude(i => i.SocialAccount)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();
        }

        public async Task<Post?> GetByExternalPostIdAsync(string externalPostId)
        {
            return await _context.Posts
                .Include(p => p.Content)
                    .ThenInclude(c => c.Brand)
                .Include(p => p.Integration)
                    .ThenInclude(i => i.SocialAccount)
                .FirstOrDefaultAsync(p => p.ExternalPostId == externalPostId && !p.IsDeleted);
        }

        public async Task<IEnumerable<Post>> GetPublishedPostsAsync()
        {
            return await _context.Posts
                .Where(p => !p.IsDeleted)
                .Include(p => p.Content)
                    .ThenInclude(c => c.Brand)
                .Include(p => p.Integration)
                    .ThenInclude(i => i.SocialAccount)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsPublishedBetweenAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Posts
                .Where(p => p.PublishedAt >= startDate && p.PublishedAt <= endDate && !p.IsDeleted)
                .Include(p => p.Content)
                    .ThenInclude(c => c.Brand)
                .Include(p => p.Integration)
                    .ThenInclude(i => i.SocialAccount)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();
        }
    }
}