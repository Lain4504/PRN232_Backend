using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class PostRepository : IPostRepository
    {
        private readonly AISAMContext _context;

        public PostRepository(AISAMContext context)
        {
            _context = context;
        }

        public async Task<Post> CreateAsync(Post post)
        {
            post.CreatedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;
            
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }
    }
}