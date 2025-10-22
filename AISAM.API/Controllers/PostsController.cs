using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Services.IServices;
using AISAM.Data.Enumeration;
using AISAM.API.Utils;
using AISAM.Common.Dtos;
using AISAM.API.Validators;
using AISAM.Common.Models;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ILogger<PostsController> _logger;
        private readonly PublishRequestValidator _publishValidator;

        public PostsController(
            IPostService postService,
            ILogger<PostsController> logger,
            PublishRequestValidator publishValidator)
        {
            _postService = postService;
            _logger = logger;
            _publishValidator = publishValidator;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<PostListItemDto>>>> GetPosts([FromQuery] Guid? brandId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] ContentStatusEnum? status = null)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _postService.GetPostsAsync(userId, brandId, page, pageSize, status);
                return Ok(GenericResponse<PagedResult<PostListItemDto>>.CreateSuccess(result, "OK"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "List posts failed");
                return StatusCode(500, GenericResponse<PagedResult<PostListItemDto>>.CreateError("Lỗi nội bộ"));
            }
        }

        [HttpGet("{postId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PostListItemDto>>> GetPostById(Guid postId)  
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var item = await _postService.GetPostByIdAsync(userId, postId);
                if (item == null)
                {
                    return NotFound(GenericResponse<PostListItemDto>.CreateError("Bài viết không tồn tại"));
                }
                return Ok(GenericResponse<PostListItemDto>.CreateSuccess(item, "OK"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get post failed");
                return StatusCode(500, GenericResponse<PostListItemDto>.CreateError("Lỗi nội bộ"));
            }
        }

        [HttpDelete("{postId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> Delete(Guid postId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var ok = await _postService.DeletePostAsync(userId, postId);
                if (!ok) return BadRequest(GenericResponse<object>.CreateError("Không thể xóa bài viết"));
                return Ok(GenericResponse<object>.CreateSuccess(null, "Đã xóa"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete post failed");
                return StatusCode(500, GenericResponse<object>.CreateError("Lỗi nội bộ"));
            }
        }
    }
}