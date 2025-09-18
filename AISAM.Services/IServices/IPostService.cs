using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface IPostService
    {
        /// <summary>
        /// Create a new post (draft or published)
        /// </summary>
        Task<PostResponseDto> CreatePostAsync(CreatePostRequest request);
    }
}