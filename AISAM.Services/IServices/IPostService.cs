using BookStore.Common.Models;

namespace BookStore.Services.IServices
{
    public interface IPostService
    {
        /// <summary>
        /// Create a new post (draft or published)
        /// </summary>
        Task<PostResponseDto> CreatePostAsync(CreatePostRequest request);
    }
}