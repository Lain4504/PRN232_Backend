using BookStore.Common;
using BookStore.Common.Models;
using BookStore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ILogger<PostController> _logger;

        public PostController(IPostService postService, ILogger<PostController> logger)
        {
            _postService = postService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new post (draft or published)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<GenericResponse<PostResponseDto>>> CreatePost([FromBody] CreatePostRequest request)
        {
            try
            {
                var result = await _postService.CreatePostAsync(request);
                return Ok(new GenericResponse<PostResponseDto>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<PostResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                return StatusCode(500, new GenericResponse<PostResponseDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Schedule a post for later publishing
        /// </summary>
        [HttpPost("schedule")]
        public async Task<ActionResult<GenericResponse<PostResponseDto>>> SchedulePost([FromBody] SchedulePostRequest request)
        {
            try
            {
                var result = await _postService.SchedulePostAsync(request);
                return Ok(new GenericResponse<PostResponseDto>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<PostResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling post");
                return StatusCode(500, new GenericResponse<PostResponseDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Publish a draft post immediately
        /// </summary>
        [HttpPost("{postId}/publish")]
        public async Task<ActionResult<GenericResponse<PostResponseDto>>> PublishPost(int postId)
        {
            try
            {
                var result = await _postService.PublishPostAsync(postId);
                return Ok(new GenericResponse<PostResponseDto>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<PostResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new GenericResponse<PostResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing post {PostId}", postId);
                return StatusCode(500, new GenericResponse<PostResponseDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get all posts for a user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<PostResponseDto>>>> GetUserPosts(int userId)
        {
            try
            {
                var posts = await _postService.GetUserPostsAsync(userId);
                return Ok(new GenericResponse<IEnumerable<PostResponseDto>>
                {
                    Success = true,
                    Data = posts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting posts for user {UserId}", userId);
                return StatusCode(500, new GenericResponse<IEnumerable<PostResponseDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get all posts for a social target
        /// </summary>
        [HttpGet("target/{socialTargetId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<PostResponseDto>>>> GetTargetPosts(int socialTargetId)
        {
            try
            {
                var posts = await _postService.GetTargetPostsAsync(socialTargetId);
                return Ok(new GenericResponse<IEnumerable<PostResponseDto>>
                {
                    Success = true,
                    Data = posts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting posts for target {TargetId}", socialTargetId);
                return StatusCode(500, new GenericResponse<IEnumerable<PostResponseDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Delete a post
        /// </summary>
        [HttpDelete("{postId}")]
        public async Task<ActionResult<GenericResponse<object>>> DeletePost(int postId)
        {
            try
            {
                var success = await _postService.DeletePostAsync(postId);
                if (success)
                {
                    return Ok(new GenericResponse<object>
                    {
                        Success = true,
                        Message = "Post deleted successfully"
                    });
                }
                else
                {
                    return NotFound(new GenericResponse<object>
                    {
                        Success = false,
                        Message = "Post not found"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", postId);
                return StatusCode(500, new GenericResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Manually trigger processing of scheduled posts (admin endpoint)
        /// </summary>
        [HttpPost("process-scheduled")]
        public async Task<ActionResult<GenericResponse<object>>> ProcessScheduledPosts()
        {
            try
            {
                await _postService.ProcessScheduledPostsAsync();
                return Ok(new GenericResponse<object>
                {
                    Success = true,
                    Message = "Scheduled posts processed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled posts");
                return StatusCode(500, new GenericResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }
}