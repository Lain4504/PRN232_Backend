using AISAM.Common;
using AISAM.Common.Dtos;
using Microsoft.AspNetCore.Mvc;
using AISAM.Services.Service;
using AISAM.Data.Enumeration;

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
        [HttpPost("{bucket}/upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<GenericResponse<UploadFileResponse>>> Upload(DefaultBucketEnum bucket, [FromForm] UploadFileRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(GenericResponse<UploadFileResponse>.CreateError("Không có file được tải lên", System.Net.HttpStatusCode.BadRequest));

            try
            {
                var fileName = await _storageService.UploadFileAsync(request.File, bucket);
                var publicUrl = _storageService.GetPublicUrl(fileName, bucket);

                var response = new UploadFileResponse
                {
                    FileName = fileName,
                    Url = publicUrl
                };

                return Ok(GenericResponse<UploadFileResponse>.CreateSuccess(response, "Upload file thành công"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(GenericResponse<UploadFileResponse>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<UploadFileResponse>.CreateError($"Lỗi khi upload file: {ex.Message}", System.Net.HttpStatusCode.InternalServerError));
            }
        }


        /// <summary>
        /// Download file
        /// </summary>
        [HttpGet("{bucket}/download")]
        public async Task<IActionResult> Download(DefaultBucketEnum bucket, [FromQuery] string fileName)
        {
            var bytes = await _storageService.DownloadFileAsync(fileName, bucket);
            var contentType = "application/octet-stream";
            var name = Path.GetFileName(fileName);

            return File(bytes, contentType, name);
        }

        /// <summary>
        /// Lấy danh sách file trong bucket (có phân trang, search, sort)
        /// </summary>
        [HttpGet("{bucket}/files")]
        public async Task<ActionResult<GenericResponse<PagedResult<FileDto>>>> ListFiles(
            DefaultBucketEnum bucket,
            [FromQuery] PaginationRequest request,
            [FromQuery] string? path = null)
        {
            try
            {
                var allFiles = await _storageService.ListFilesAsync(bucket, path);

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

                return Ok(GenericResponse<PagedResult<FileDto>>.CreateSuccess(result, "Lấy danh sách file thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<PagedResult<FileDto>>.CreateError($"Lỗi khi lấy danh sách file: {ex.Message}", System.Net.HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Xóa file
        /// </summary>
        [HttpDelete("{bucket}/files")]
        public async Task<ActionResult<GenericResponse<bool>>> Remove(DefaultBucketEnum bucket, [FromBody] string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0)
                return BadRequest(GenericResponse<bool>.CreateError("Không có tên file được cung cấp", System.Net.HttpStatusCode.BadRequest));

            try
            {
                await _storageService.RemoveFilesAsync(bucket, fileNames);
                return Ok(GenericResponse<bool>.CreateSuccess(true, "Xóa file thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<bool>.CreateError($"Lỗi khi xóa file: {ex.Message}", System.Net.HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Tạo signed URL
        /// </summary>
        [HttpGet("{bucket}/signed-url")]
        public async Task<ActionResult<GenericResponse<SignedUrlResponse>>> SignedUrl(DefaultBucketEnum bucket, [FromQuery] string fileName, [FromQuery] int expires = 3600)
        {
            try
            {
                var url = await _storageService.CreateSignedUrlAsync(fileName, bucket, expires);
                var response = new SignedUrlResponse { Url = url };
                return Ok(GenericResponse<SignedUrlResponse>.CreateSuccess(response, "Tạo signed URL thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<SignedUrlResponse>.CreateError($"Lỗi khi tạo signed URL: {ex.Message}", System.Net.HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Lấy public URL
        /// </summary>
        [HttpGet("{bucket}/public-url")]
        public ActionResult<GenericResponse<PublicUrlResponse>> PublicUrl(DefaultBucketEnum bucket, [FromQuery] string fileName)
        {
            try
            {
                var url = _storageService.GetPublicUrl(fileName, bucket);
                var response = new PublicUrlResponse { Url = url };
                return Ok(GenericResponse<PublicUrlResponse>.CreateSuccess(response, "Lấy public URL thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<PublicUrlResponse>.CreateError($"Lỗi khi lấy public URL: {ex.Message}", System.Net.HttpStatusCode.InternalServerError));
            }
        }
    }
}

public class UploadFileRequest
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = default!;
}

public class UploadFileResponse
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class SignedUrlResponse
{
    public string Url { get; set; } = string.Empty;
}

public class PublicUrlResponse
{
    public string Url { get; set; } = string.Empty;
}