using Microsoft.AspNetCore.Http;

public class SupabaseStorageService
{
    private readonly Supabase.Client _supabase;

    public SupabaseStorageService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    // Upload file
    public async Task UploadFileAsync(string bucket, IFormFile formFile, string objectPath, bool upsert = false)
    {
        using var ms = new MemoryStream();
        await formFile.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var options = new Supabase.Storage.FileOptions
        {
            ContentType = formFile.ContentType,
            Upsert = upsert
        };

        await _supabase.Storage.From(bucket).Upload(bytes, objectPath, options);
    }

    // Download file
    public async Task<byte[]> DownloadFileAsync(string bucket, string objectPath)
    {
        var result = await _supabase.Storage.From(bucket).Download(objectPath, null);
        return result;
    }

    // List files
    public async Task<List<Supabase.Storage.FileObject>?> ListFilesAsync(string bucket, string? prefix = null)
    => await _supabase.Storage.From(bucket).List(prefix);

    // Replace file
    public async Task ReplaceFileAsync(string bucket, IFormFile newFile, string objectPath)
    {
        using var ms = new MemoryStream();
        await newFile.CopyToAsync(ms);
        var bytes = ms.ToArray();
        await _supabase.Storage.From(bucket).Update(bytes, objectPath);
    }

    // Move file
    public async Task MoveFileAsync(string bucket, string fromPath, string toPath)
        => await _supabase.Storage.From(bucket).Move(fromPath, toPath);

    // Delete files
    public async Task RemoveFilesAsync(string bucket, IEnumerable<string> paths)
        => await _supabase.Storage.From(bucket).Remove(new List<string>(paths));

    // Create signed URL
    public async Task<string?> CreateSignedUrlAsync(string bucket, string objectPath, int expiresInSeconds = 60)
    {
        return await _supabase.Storage.From(bucket).CreateSignedUrl(objectPath, expiresInSeconds);
    }

    // Get public URL
    public string GetPublicUrl(string bucket, string objectPath)
        => _supabase.Storage.From(bucket).GetPublicUrl(objectPath);
}
