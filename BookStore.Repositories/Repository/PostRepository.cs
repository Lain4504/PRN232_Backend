using BookStore.Common.Enumeration;
using BookStore.Data;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repositories.Repository
{
    public class PostRepository : IPostRepository
    {
        private readonly BookStoreDbContext _dbContext;

        public PostRepository(BookStoreDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Post> CreateAsync(Post post, CancellationToken cancellationToken = default)
        {
            _dbContext.Posts.Add(post);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return post;
        }

        public async Task<Post> UpdateAsync(Post post, CancellationToken cancellationToken = default)
        {
            _dbContext.Posts.Update(post);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return post;
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var post = await _dbContext.Posts.FindAsync(new object[] { id }, cancellationToken);
            if (post != null)
            {
                _dbContext.Posts.Remove(post);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<Post>> GetAllPostsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Posts
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Post>> GetPostsByCategoryAsync(PostCategoryEnum category, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Posts
                .AsNoTracking()
                .Where(p => p.Category == category)
                .ToListAsync(cancellationToken);
        }
    }
}