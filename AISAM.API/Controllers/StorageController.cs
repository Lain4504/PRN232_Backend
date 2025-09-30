using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/storage")]
public class StorageController : ControllerBase
{
    private readonly SupabaseStorageService _svc;
    public StorageController(SupabaseStorageService svc) => _svc = svc;

    [HttpPost("{bucket}/upload")]
    public async Task<IActionResult> UploadFile(string bucket, IFormFile file, [FromForm] string path, [FromForm] bool upsert = false)
    {
        await _svc.UploadFileAsync(bucket, file, path, upsert);
        return Ok(new { path });
    }

    [HttpGet("{bucket}/files")]
    public async Task<IActionResult> ListFiles(string bucket, [FromQuery] string? prefix = null)
        => Ok(await _svc.ListFilesAsync(bucket, prefix));

    [HttpGet("{bucket}/download")]
    public async Task<IActionResult> Download(string bucket, [FromQuery] string path)
    {
        var bytes = await _svc.DownloadFileAsync(bucket, path);
        var name = Path.GetFileName(path);
        return File(bytes, "application/octet-stream", name);
    }

    [HttpPost("{bucket}/replace")]
    public async Task<IActionResult> Replace(string bucket, IFormFile file, [FromForm] string path)
    {
        await _svc.ReplaceFileAsync(bucket, file, path);
        return Ok();
    }

    [HttpPost("{bucket}/move")]
    public async Task<IActionResult> Move(string bucket, [FromBody] MoveDto dto)
    {
        await _svc.MoveFileAsync(bucket, dto.From, dto.To);
        return Ok();
    }

    [HttpPost("{bucket}/remove")]
    public async Task<IActionResult> RemoveFiles(string bucket, [FromBody] List<string> paths)
    {
        await _svc.RemoveFilesAsync(bucket, paths);
        return NoContent();
    }

    [HttpGet("{bucket}/signed-url")]
    public async Task<IActionResult> SignedUrl(string bucket, [FromQuery] string path, [FromQuery] int expires = 60)
    {
        var s = await _svc.CreateSignedUrlAsync(bucket, path, expires);
        return Ok(new { url = s });
    }

    [HttpGet("{bucket}/public-url")]
    public IActionResult PublicUrl(string bucket, [FromQuery] string path)
    {
        var url = _svc.GetPublicUrl(bucket, path);
        return Ok(new { url });
    }

    public record MoveDto(string From, string To);
}
