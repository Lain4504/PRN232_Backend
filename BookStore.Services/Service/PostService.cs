using BookStore.Common.Enumeration;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using BookStore.Services.IServices;

namespace BookStore.Services.Service
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;

        public PostService(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return _postRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<Post> CreatePostAsync(Post post, CancellationToken cancellationToken = default)
        {
            return await _postRepository.CreateAsync(post, cancellationToken);
        }

        public async Task<Post?> UpdatePostAsync(Post post, CancellationToken cancellationToken = default)
        {
            return await _postRepository.UpdateAsync(post, cancellationToken);
        }

        public async Task DeletePostAsync(string id, CancellationToken cancellationToken = default)
        {
            await _postRepository.DeleteAsync(id, cancellationToken);
        }

        public async Task<IEnumerable<Post>> GetAllPostsAsync(CancellationToken cancellationToken = default)
        {
            return await _postRepository.GetAllPostsAsync(cancellationToken);
        }

        public async Task<IEnumerable<Post>> GetPostsByCategoryAsync(PostCategoryEnum category, CancellationToken cancellationToken = default)
        {
            return await _postRepository.GetPostsByCategoryAsync(category, cancellationToken);
        }
    }
}