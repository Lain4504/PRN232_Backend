using AISAM.Common;
using AISAM.Common.Models;
using AISAM.Services.IServices;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ILogger<PostController> _logger;
        private readonly IValidator<CreatePostRequest> _createPostValidator;

        public PostController(IPostService postService, ILogger<PostController> logger, IValidator<CreatePostRequest> createPostValidator)
        {
            _postService = postService;
            _logger = logger;
            _createPostValidator = createPostValidator;
        }

        /// <summary>
        /// Create a new post (draft or published)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<GenericResponse<PostResponseDto>>> CreatePost([FromBody] CreatePostRequest request)
        {
            try
            {
                // Manual FluentValidation to return GenericResponse with detailed validation errors
                ValidationResult validationResult = await _createPostValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var flatList = validationResult.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
                    var bad = GenericResponse<PostResponseDto>.CreateError(
                        "FluentValidation failed",
                        System.Net.HttpStatusCode.BadRequest,
                        "FLUENT_VALIDATION_ERROR");
                    bad.Error.ValidationErrors = new Dictionary<string, List<string>>
                    {
                        { "FluentValidationErrors", flatList }
                    };
                    return StatusCode(bad.StatusCode, bad);
                }

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