using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Services.IServices;
using AISAM.Data.Enumeration;
using AISAM.API.Utils;
using AISAM.Common.Dtos;
using AISAM.API.Validators;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ILogger<PostsController> _logger;
        private readonly PublishRequestValidator _publishValidator;
        private readonly ScheduleRequestValidator _scheduleValidator;

        public PostsController(
            IPostService postService,
            ILogger<PostsController> logger,
            PublishRequestValidator publishValidator,
            ScheduleRequestValidator scheduleValidator)
        {
            _postService = postService;
            _logger = logger;
            _publishValidator = publishValidator;
            _scheduleValidator = scheduleValidator;
        }

        [HttpPost("/api/contents/{contentId}/publish")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PublishResultDto>>> Publish(Guid contentId, [FromBody] PublishRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                // Validate request using the validator
                var validationResult = await _publishValidator.ValidateAsync(request, contentId, userId);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(GenericResponse<PublishResultDto>.CreateError(errors));
                }

                var result = await _postService.PublishPostAsync(userId, contentId, request.IntegrationIds);
                return Ok(GenericResponse<PublishResultDto>.CreateSuccess(result, result.Success ? "Đăng bài thành công" : "Đăng bài thất bại"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<PublishResultDto>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(GenericResponse<PublishResultDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Xuất bản thất bại");
                return StatusCode(500, GenericResponse<PublishResultDto>.CreateError("Lỗi nội bộ"));
            }
        }

        [HttpPost("/api/contents/{contentId}/schedule")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> Schedule(Guid contentId, [FromBody] ScheduleRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                // Validate request using the validator
                var validationResult = await _scheduleValidator.ValidateAsync(request, contentId, userId);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(GenericResponse<object>.CreateError(errors));
                }

                var id = await _postService.SchedulePostAsync(userId, contentId, request.IntegrationId, request.ScheduledAtUtc);
                return Ok(GenericResponse<object>.CreateSuccess(new { scheduleId = id }, "Đã lên lịch"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lên lịch thất bại");
                return StatusCode(500, GenericResponse<object>.CreateError("Lỗi nội bộ"));
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<PostListItemDto>>>> List([FromQuery] Guid? brandId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] ContentStatusEnum? status = null)
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
        public async Task<ActionResult<GenericResponse<PostListItemDto>>> Get(Guid postId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var list = await _postService.GetPostsAsync(userId, null, 1, 1);
                var item = list.Data.FirstOrDefault(p => p.Id == postId);
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
                var ok = await _postService.DeleteOrCancelPostAsync(userId, postId);
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

        [HttpPost("{postId}/pull-reports")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> PullReports(Guid postId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var ok = await _postService.PullReportsAsync(userId, postId);
                return Ok(GenericResponse<object>.CreateSuccess(null, ok ? "Thành công" : "Thất bại"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pull reports failed");
                return StatusCode(500, GenericResponse<object>.CreateError("Lỗi nội bộ"));
            }
        }

        [HttpPut("re-auth-integration/{integrationId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> ReAuth(Guid integrationId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var ok = await _postService.ReAuthIntegrationAsync(userId, integrationId);
                return Ok(GenericResponse<object>.CreateSuccess(null, ok ? "Thành công" : "Thất bại"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReAuth failed");
                return StatusCode(500, GenericResponse<object>.CreateError("Lỗi nội bộ"));
            }
        }

        public class PublishRequest
        {
            public IEnumerable<Guid> IntegrationIds { get; set; } = Array.Empty<Guid>();
        }

        public class ScheduleRequest
        {
            public Guid IntegrationId { get; set; }
            public DateTime ScheduledAtUtc { get; set; }
        }
    }
}