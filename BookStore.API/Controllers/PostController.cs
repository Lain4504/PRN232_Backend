using System.Net;
using AutoMapper;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;
using BookStore.Common;
using BookStore.Data.Model;
using BookStore.Services.IServices;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/posts")]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreatePostRequestDto> _validator;
    private readonly IImageUploadService _imageUploadService;

    public PostController(IPostService postService, IMapper mapper, IValidator<CreatePostRequestDto> validator, IImageUploadService imageUploadService)
    {
        _postService = postService;
        _mapper = mapper;
        _validator = validator;
        _imageUploadService = imageUploadService;
    }

    // AutoMapper - trả về đầy đủ fields
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var post = await _postService.GetByIdAsync(id, cancellationToken);
        if (post == null)
        {
            var notFound = GenericResponse<PostResponseDto>.CreateError("Post not found", HttpStatusCode.NotFound, "POST_NOT_FOUND");
            return StatusCode(notFound.StatusCode, notFound);
        }

        // AutoMapper mapping - đầy đủ fields
        var dto = _mapper.Map<PostResponseDto>(post);

        var response = GenericResponse<PostResponseDto>.CreateSuccess(dto);
        return Ok(response);
    }

    // API Create Post với FluentValidation (title + content)
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequestDto request, CancellationToken cancellationToken)
    {
        // Sử dụng FluentValidation để validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            var badRequest = GenericResponse<object>.CreateError(
                "FluentValidation failed",
                HttpStatusCode.BadRequest,
                "FLUENT_VALIDATION_ERROR");

            // Thêm FluentValidation errors vào ErrorDetails
            badRequest.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "FluentValidationErrors", errors }
            };

            return StatusCode(badRequest.StatusCode, badRequest);
        }

        try
        {
            // Sử dụng AutoMapper để map từ DTO sang EntityDTO
            var post = _mapper.Map<Post>(request);

            // Tạo post
            var createdPost = await _postService.CreatePostAsync(post, cancellationToken);

            // Map lại sang DTO để trả về
            var responseDto = _mapper.Map<PostResponseDto>(createdPost);

            var response = GenericResponse<PostResponseDto>.CreateSuccess(responseDto);
            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Fail to create post",
                HttpStatusCode.InternalServerError,
                "CREATE_POST_ERROR");

            // Thêm exception message vào ErrorDetails
            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionMessage", new List<string> { ex.Message } }
            };

            return StatusCode(error.StatusCode, error);
        }
    }

    // API Create Post with Thumbnail Upload (All-in-one approach)
    [HttpPost("create-with-thumbnail")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreatePostWithThumbnail(
        string title,
        string content,
        string? brief,
        IFormFile? thumbnail,
        CancellationToken cancellationToken)
    {
        // Validate basic fields
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            var badRequest = GenericResponse<object>.CreateError(
                "Title and Content are required",
                HttpStatusCode.BadRequest,
                "REQUIRED_FIELDS_MISSING");

            return BadRequest(badRequest);
        }

        try
        {
            string? thumbnailUrl = null;

            // Upload thumbnail if provided
            if (thumbnail != null && thumbnail.Length > 0)
            {
                // Validate thumbnail
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(thumbnail.ContentType.ToLower()))
                {
                    var badRequest = GenericResponse<object>.CreateError(
                        "Only image files are allowed for thumbnail",
                        HttpStatusCode.BadRequest,
                        "INVALID_THUMBNAIL_TYPE");

                    return BadRequest(badRequest);
                }

                const int maxSizeInBytes = 5 * 1024 * 1024; // 5MB
                if (thumbnail.Length > maxSizeInBytes)
                {
                    var badRequest = GenericResponse<object>.CreateError(
                        "Thumbnail size must be less than 5MB",
                        HttpStatusCode.BadRequest,
                        "THUMBNAIL_TOO_LARGE");

                    return BadRequest(badRequest);
                }

                thumbnailUrl = await _imageUploadService.UploadImageAsync(thumbnail, "post-thumbnails");
            }

            // Create post with thumbnail
            var post = new Post
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Content = content,
                Brief = brief ?? string.Empty,
                Thumbnail = thumbnailUrl,
                CreatedAt = DateTime.UtcNow,
                Status = BookStore.Common.Enumeration.PostStatusEnum.Draft,
                Category = BookStore.Common.Enumeration.PostCategoryEnum.News
            };

            var createdPost = await _postService.CreatePostAsync(post, cancellationToken);
            var responseDto = _mapper.Map<PostResponseDto>(createdPost);

            var response = GenericResponse<PostResponseDto>.CreateSuccess(responseDto);
            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Failed to create post with thumbnail",
                HttpStatusCode.InternalServerError,
                "CREATE_POST_ERROR");

            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionMessage", new List<string> { ex.Message } }
            };

            return StatusCode(error.StatusCode, error);
        }
    }

    // Get All Posts
    [HttpGet]
    public async Task<IActionResult> GetAllPosts(CancellationToken cancellationToken)
    {
        try
        {
            var posts = await _postService.GetAllPostsAsync(cancellationToken);
            var postDtos = _mapper.Map<IEnumerable<PostResponseDto>>(posts);

            var response = GenericResponse<IEnumerable<PostResponseDto>>.CreateSuccess(postDtos);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Failed to get posts",
                HttpStatusCode.InternalServerError,
                "GET_POSTS_ERROR");

            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionMessage", new List<string> { ex.Message } }
            };

            return StatusCode(error.StatusCode, error);
        }
    }

    // Update Post
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost([FromRoute] string id, [FromBody] CreatePostRequestDto request, CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            var badRequest = GenericResponse<object>.CreateError(
                "Validation failed",
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR");

            badRequest.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ValidationErrors", errors }
            };

            return StatusCode(badRequest.StatusCode, badRequest);
        }

        try
        {
            // Check if post exists
            var existingPost = await _postService.GetByIdAsync(id, cancellationToken);
            if (existingPost == null)
            {
                var notFound = GenericResponse<object>.CreateError(
                    "Post not found",
                    HttpStatusCode.NotFound,
                    "POST_NOT_FOUND");

                return StatusCode(notFound.StatusCode, notFound);
            }

            // Update post properties
            existingPost.Title = request.Title;
            existingPost.Content = request.Content;
            // Keep other properties like UserId, CreatedAt, etc.

            var updatedPost = await _postService.UpdatePostAsync(existingPost, cancellationToken);
            var responseDto = _mapper.Map<PostResponseDto>(updatedPost);

            var response = GenericResponse<PostResponseDto>.CreateSuccess(responseDto);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Failed to update post",
                HttpStatusCode.InternalServerError,
                "UPDATE_POST_ERROR");

            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionMessage", new List<string> { ex.Message } }
            };

            return StatusCode(error.StatusCode, error);
        }
    }

    // Delete Post
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost([FromRoute] string id, CancellationToken cancellationToken)
    {
        try
        {
            // Check if post exists
            var existingPost = await _postService.GetByIdAsync(id, cancellationToken);
            if (existingPost == null)
            {
                var notFound = GenericResponse<object>.CreateError(
                    "Post not found",
                    HttpStatusCode.NotFound,
                    "POST_NOT_FOUND");

                return StatusCode(notFound.StatusCode, notFound);
            }

            await _postService.DeletePostAsync(id, cancellationToken);

            var response = GenericResponse<object>.CreateSuccess(
                new { Message = "Post deleted successfully" });

            return Ok(response);
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Failed to delete post",
                HttpStatusCode.InternalServerError,
                "DELETE_POST_ERROR");

            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionMessage", new List<string> { ex.Message } }
            };

            return StatusCode(error.StatusCode, error);
        }
    }

    [HttpPost("upload-thumbnail")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadThumbnail(IFormFile file)
    {
        // Validation: Check if file is provided
        if (file == null || file.Length == 0)
        {
            var badRequest = GenericResponse<object>.CreateError(
                "No file provided or file is empty",
                HttpStatusCode.BadRequest,
                "FILE_REQUIRED");

            return BadRequest(badRequest);
        }

        // Validation: Check file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            var badRequest = GenericResponse<object>.CreateError(
                "Only image files are allowed (JPEG, PNG, GIF, WebP)",
                HttpStatusCode.BadRequest,
                "INVALID_FILE_TYPE");

            return BadRequest(badRequest);
        }

        // Validation: Check file size (max 5MB)
        const int maxSizeInBytes = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxSizeInBytes)
        {
            var badRequest = GenericResponse<object>.CreateError(
                "File size must be less than 5MB",
                HttpStatusCode.BadRequest,
                "FILE_TOO_LARGE");

            return BadRequest(badRequest);
        }

        try
        {
            var imageUrl = await _imageUploadService.UploadImageAsync(file, "post-thumbnails");

            var response = GenericResponse<object>.CreateSuccess(
                new
                {
                    ThumbnailUrl = imageUrl,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType
                },
                "Thumbnail uploaded successfully"
            );

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            // Handle validation errors from service
            var badRequest = GenericResponse<object>.CreateError(
                ex.Message,
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR");

            return BadRequest(badRequest);
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Failed to upload thumbnail",
                HttpStatusCode.InternalServerError,
                "UPLOAD_ERROR");

            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionMessage", new List<string> { ex.Message } }
            };

            return StatusCode(500, error);
        }
    }
}
