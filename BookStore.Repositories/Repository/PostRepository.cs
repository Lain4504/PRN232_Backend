using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repositories.Repository
{
    public class PostRepository : IPostRepository
    {
        private readonly BookStoreContext _context;

        public PostRepository(BookStoreContext context)
        {
            _context = context;
        }

        public async Task<Post?> GetByIdAsync(int id)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.SocialAccount)
                .Include(p => p.SocialTarget)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Post?> GetByProviderPostIdAsync(string providerPostId)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.SocialAccount)
                .Include(p => p.SocialTarget)
                .FirstOrDefaultAsync(p => p.ProviderPostId == providerPostId);
        }

        public async Task<IEnumerable<Post>> GetByUserIdAsync(int userId)
        {
            return await _context.Posts
                .Where(p => p.UserId == userId)
                .Include(p => p.SocialAccount)
                .Include(p => p.SocialTarget)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetBySocialAccountIdAsync(int socialAccountId)
        {
            return await _context.Posts
                .Where(p => p.SocialAccountId == socialAccountId)
                .Include(p => p.User)
                .Include(p => p.SocialAccount)
                .Include(p => p.SocialTarget)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetBySocialTargetIdAsync(int socialTargetId)
        {
            return await _context.Posts
                .Where(p => p.SocialTargetId == socialTargetId)
                .Include(p => p.User)
                .Include(p => p.SocialAccount)
                .Include(p => p.SocialTarget)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetScheduledPostsAsync()
        {
            return await _context.Posts
                .Where(p => p.Status == PostStatus.Scheduled && 
                           p.ScheduledTime <= DateTime.UtcNow)
                .Include(p => p.User)
                .Include(p => p.SocialAccount)
                .Include(p => p.SocialTarget)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetByStatusAsync(PostStatus status)
        {
            return await _context.Posts
                .Where(p => p.Status == status)
                .Include(p => p.User)
                .Include(p => p.SocialAccount)
                .Include(p => p.SocialTarget)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Post> CreateAsync(Post post)
        {
            post.CreatedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;
            
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task UpdateAsync(Post post)
        {
            post.UpdatedAt = DateTime.UtcNow;
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Posts.AnyAsync(p => p.Id == id);
        }
    }
}