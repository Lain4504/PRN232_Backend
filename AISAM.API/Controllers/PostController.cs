using AISAM.API.Filters;
using AISAM.API.Utils;
using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ILogger<PostController> _logger;

        public PostController(IPostService postService, ILogger<PostController> logger)
        {
            _postService = postService;
            _logger = logger;
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidationFilter))]
        public async Task<ActionResult<GenericResponse<PostResponseDto>>> CreatePost([FromBody] CreatePostRequest request)
        {
            var result = await _postService.CreatePostAsync(request);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            var responseDto = MapToResponseDto(result.Data!);
            var response = GenericResponse<PostResponseDto>.CreateSuccess(responseDto, result.Message);

            return StatusCode(result.StatusCode, response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GenericResponse<PostResponseDto>>> GetPost(Guid id)
        {
            var result = await _postService.GetPostByIdAsync(id);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            var responseDto = MapToResponseDto(result.Data!);
            var response = GenericResponse<PostResponseDto>.CreateSuccess(responseDto, result.Message);

            return StatusCode(result.StatusCode, response);
        }

        /// <summary>
        /// Lấy danh sách post của user hiện tại phân trang
        /// GET api/posts?page=1&pageSize=10
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<PostResponseDto>>>> GetAllPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var paginationRequest = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _postService.GetPagedByUserIdAsync(userId, paginationRequest);
                return Ok(GenericResponse<PagedResult<PostResponseDto>>.CreateSuccess(result, "Lấy danh sách post thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<PagedResult<PostResponseDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated posts");
                return StatusCode(500, GenericResponse<PagedResult<PostResponseDto>>.CreateError("Đã xảy ra lỗi khi lấy danh sách post"));
            }
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidationFilter))]
        public async Task<ActionResult<GenericResponse<PostResponseDto>>> UpdatePost(Guid id, [FromBody] UpdatePostRequest request)
        {
            var result = await _postService.UpdatePostAsync(id, request);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            var responseDto = MapToResponseDto(result.Data!);
            var response = GenericResponse<PostResponseDto>.CreateSuccess(responseDto, result.Message);

            return StatusCode(result.StatusCode, response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<GenericResponse<bool>>> DeletePost(Guid id)
        {
            var result = await _postService.DeletePostAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}/soft-delete")]
        public async Task<ActionResult<GenericResponse<bool>>> SoftDeletePost(Guid id)
        {
            var result = await _postService.SoftDeletePostAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{id}/restore")]
        public async Task<ActionResult<GenericResponse<bool>>> RestorePost(Guid id)
        {
            var result = await _postService.RestorePostAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        // Published posts tracking endpoints
        [HttpGet("content/{contentId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<PostResponseDto>>>> GetPostsByContentId(Guid contentId)
        {
            var result = await _postService.GetPostsByContentIdAsync(contentId);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            var responseDtos = result.Data!.Select(MapToResponseDto);
            var response = GenericResponse<IEnumerable<PostResponseDto>>.CreateSuccess(responseDtos, result.Message);

            return StatusCode(result.StatusCode, response);
        }

        [HttpGet("integration/{integrationId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<PostResponseDto>>>> GetPostsByIntegrationId(Guid integrationId)
        {
            var result = await _postService.GetPostsByIntegrationIdAsync(integrationId);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            var responseDtos = result.Data!.Select(MapToResponseDto);
            var response = GenericResponse<IEnumerable<PostResponseDto>>.CreateSuccess(responseDtos, result.Message);

            return StatusCode(result.StatusCode, response);
        }

        [HttpGet("external/{externalPostId}")]
        public async Task<ActionResult<GenericResponse<PostResponseDto>>> GetPostByExternalId(string externalPostId)
        {
            var result = await _postService.GetPostByExternalIdAsync(externalPostId);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            var responseDto = MapToResponseDto(result.Data!);
            var response = GenericResponse<PostResponseDto>.CreateSuccess(responseDto, result.Message);

            return StatusCode(result.StatusCode, response);
        }

        [HttpGet("published")]
        public async Task<ActionResult<GenericResponse<IEnumerable<PostResponseDto>>>> GetPublishedPosts()
        {
            var result = await _postService.GetPublishedPostsAsync();

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            var responseDtos = result.Data!.Select(MapToResponseDto);
            var response = GenericResponse<IEnumerable<PostResponseDto>>.CreateSuccess(responseDtos, result.Message);

            return StatusCode(result.StatusCode, response);
        }

        [HttpGet("published/between")]
        public async Task<ActionResult<GenericResponse<IEnumerable<PostResponseDto>>>> GetPostsPublishedBetween([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var result = await _postService.GetPostsPublishedBetweenAsync(startDate, endDate);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            var responseDtos = result.Data!.Select(MapToResponseDto);
            var response = GenericResponse<IEnumerable<PostResponseDto>>.CreateSuccess(responseDtos, result.Message);

            return StatusCode(result.StatusCode, response);
        }

        private static PostResponseDto MapToResponseDto(Post post)
        {
            return new PostResponseDto
            {
                Id = post.Id,
                ContentId = post.ContentId,
                IntegrationId = post.IntegrationId,
                ExternalPostId = post.ExternalPostId,
                PublishedAt = post.PublishedAt,
                Status = post.Status,
                IsDeleted = post.IsDeleted,
                CreatedAt = post.CreatedAt,
                Content = post.Content != null ? new ContentInfoDto
                {
                    Id = post.Content.Id,
                    Title = post.Content.Title,
                    TextContent = post.Content.TextContent,
                    AdType = post.Content.AdType,
                    Status = post.Content.Status,
                    BrandName = post.Content.Brand?.Name
                } : null,
                Integration = post.Integration != null ? new IntegrationInfoDto
                {
                    Id = post.Integration.Id,
                    PlatformName = post.Integration.Platform.ToString(),
                    AccountName = post.Integration.SocialAccount?.AccountId ?? "Unknown",
                    IsActive = post.Integration.IsActive
                } : null
            };
        }
    }
}