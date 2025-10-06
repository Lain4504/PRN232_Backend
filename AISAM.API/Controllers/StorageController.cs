using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;
using AISAM.Services.Service;
using AISAM.Common.Models;

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
    }
}

public class UploadFileRequest
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = default!;
}