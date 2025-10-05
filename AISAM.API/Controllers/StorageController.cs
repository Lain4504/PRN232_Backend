using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Services.Service;
using AISAM.Common.Models;
using System.Security.Claims;

namespace AISAM.Api.Controllers
{
    [ApiController]
    [Route("api/storage")]
    public class StorageController : ControllerBase
    {
        private readonly SupabaseStorageService _storageService;

        public StorageController(SupabaseStorageService storageService)
        {
            _storageService = storageService;
        }

        /// <summary>
        /// Upload avatar image with validation, trả về public URL
        /// </summary>
        [HttpPost("upload-avatar")]
        [Consumes("multipart/form-data")]
        [Authorize]
        [ApiExplorerSettings(IgnoreApi = true)] // Temporarily hide from Swagger
        public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarRequest request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Invalid file type. Only image files are allowed." });
            }            // Validate file size (5MB max)
            const int maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { message = "File size too large. Maximum 5MB allowed." });
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            // Generate avatar-specific filename
            var uniqueFileName = $"avatars/{userId}_{Guid.NewGuid()}{fileExtension}";

            try
            {
                var fileName = await _storageService.UploadFileAsync(file.OpenReadStream(), uniqueFileName, file.ContentType);
                var publicUrl = _storageService.GetPublicUrl(fileName);

                return Ok(new { fileName, url = publicUrl, message = "Avatar uploaded successfully" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error uploading avatar" });
            }
        }

        /// <summary>
        /// Upload file (ảnh/video/tài liệu...), trả về fileName + public url
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")] // ✅ fix swagger hiển thị upload file
        public async Task<IActionResult> Upload([FromForm] UploadFileRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded");

            var fileName = await _storageService.UploadFileAsync(request.File);
            var publicUrl = _storageService.GetPublicUrl(fileName);

            return Ok(new { fileName, url = publicUrl });
        }

        /// <summary>
        /// Download file
        /// </summary>
        [HttpGet("download")]
        public async Task<IActionResult> Download([FromQuery] string fileName)
        {
            var bytes = await _storageService.DownloadFileAsync(fileName);
            var contentType = "application/octet-stream";
            var name = Path.GetFileName(fileName);

            return File(bytes, contentType, name);
        }

        /// <summary>
        /// Lấy danh sách file trong bucket (có phân trang, search, sort)
        /// </summary>
        [HttpGet("{bucket}/files")]
        public async Task<IActionResult> ListFiles(
            string bucket,
            [FromQuery] PaginationRequest request,
            [FromQuery] string? path = null)
        {
            var allFiles = await _storageService.ListFilesAsync(path);

            var query = allFiles.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(f =>
                    f.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDescending ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),
                    "lastmodified" => request.SortDescending ? query.OrderByDescending(f => f.LastModified) : query.OrderBy(f => f.LastModified),
                    "size" => request.SortDescending ? query.OrderByDescending(f => f.Size) : query.OrderBy(f => f.Size),
                    _ => query
                };
            }

            var totalCount = query.Count();
            var data = query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var result = new PagedResult<FileDto>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return Ok(result);
        }

        [HttpDelete("files")]
        public async Task<IActionResult> Remove([FromBody] string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0)
                return BadRequest("No file names provided");

            await _storageService.RemoveFilesAsync(fileNames);
            return NoContent();
        }

        [HttpGet("signed-url")]
        public async Task<IActionResult> SignedUrl([FromQuery] string fileName, [FromQuery] int expires = 3600)
        {
            var url = await _storageService.CreateSignedUrlAsync(fileName, expires);
            return Ok(new { url });
        }

        [HttpGet("public-url")]
        public IActionResult PublicUrl([FromQuery] string fileName)
        {
            var url = _storageService.GetPublicUrl(fileName);
            return Ok(new { url });
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return userId;
            }
            return null;
        }
    }
}

public class UploadFileRequest
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = default!;
}

public class UploadAvatarRequest
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = default!;
}