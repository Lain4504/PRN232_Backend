using BookStore.Common.Models;

namespace BookStore.Services.IServices
{
    public interface IPostService
    {
        /// <summary>
        /// Create a new post (draft or published)
        /// </summary>
        Task<PostResponseDto> CreatePostAsync(CreatePostRequest request);
        
        /// <summary>
        /// Schedule a post for later publishing
        /// </summary>
        Task<PostResponseDto> SchedulePostAsync(SchedulePostRequest request);
        
        /// <summary>
        /// Publish a draft post immediately
        /// </summary>
        Task<PostResponseDto> PublishPostAsync(int postId);
        
        /// <summary>
        /// Get all posts for a user
        /// </summary>
        Task<IEnumerable<PostResponseDto>> GetUserPostsAsync(int userId);
        
        /// <summary>
        /// Get all posts for a social target
        /// </summary>
        Task<IEnumerable<PostResponseDto>> GetTargetPostsAsync(int socialTargetId);
        
        /// <summary>
        /// Delete a post
        /// </summary>
        Task<bool> DeletePostAsync(int postId);
        
        /// <summary>
        /// Process scheduled posts (background service)
        /// </summary>
        Task ProcessScheduledPostsAsync();
    }
}