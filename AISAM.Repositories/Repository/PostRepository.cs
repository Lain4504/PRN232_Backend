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

        public async Task<SocialPost> CreateAsync(SocialPost socialPost)
        {
            socialPost.CreatedAt = DateTime.UtcNow;
            socialPost.UpdatedAt = DateTime.UtcNow;
            
            _context.Posts.Add(socialPost);
            await _context.SaveChangesAsync();
            return socialPost;
        }
    }
}