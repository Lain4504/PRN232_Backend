using AISAM.Common;
using AISAM.Common.Models;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace AISAM.API.Controllers
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
    }
}