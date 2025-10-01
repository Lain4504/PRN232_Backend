// Aliases để tránh conflict tên
using SupabaseClient = Supabase.Client;
using StorageFileOptions = Supabase.Storage.FileOptions;
using StorageTransformOptions = Supabase.Storage.TransformOptions;
using StorageFileObject = Supabase.Storage.FileObject;
using AISAM.Common.Models;

namespace AISAM.Services.Service
{
    public class SupabaseStorageService
    {
        private readonly SupabaseClient _supabaseClient;
        private readonly string _bucketName;

        public SupabaseStorageService(SupabaseClient supabaseClient, string bucketName = "image")
        {
            _supabaseClient = supabaseClient ?? throw new ArgumentNullException(nameof(supabaseClient));
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
        }

        /// <summary>
        /// Sinh tên file ngẫu nhiên.
        /// </summary>
        private static string GenerateUniqueFileName(string originalFileName)
        {
            var ext = Path.GetExtension(originalFileName) ?? string.Empty;
            var name = Guid.NewGuid().ToString("N");
            return $"{name}{ext}";
        }

        /// <summary>
        /// Upload file: đọc stream thành byte[] (vì SDK cần byte[]), set ContentType chính xác.
        /// Trả về tên file lưu trên bucket
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string originalFileName, string contentType)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("originalFileName required", nameof(originalFileName));

            // đọc stream thành byte[]
            await using var ms = new MemoryStream();
            // nếu stream có Position, reset về 0 trước khi copy
            if (fileStream.CanSeek)
                fileStream.Position = 0;
            await fileStream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var safeFileName = GenerateUniqueFileName(originalFileName);

            var bucket = _supabaseClient.Storage.From(_bucketName);

            // Sử dụng overload Upload(byte[] bytes, string path, FileOptions options)
            await bucket.Upload(bytes, safeFileName, new StorageFileOptions
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                Upsert = false // không ghi đè
            });

            return safeFileName;
        }

        /// <summary>
        /// Helper tiện ích: upload từ IFormFile
        /// </summary>
        public async Task<string> UploadFileAsync(Microsoft.AspNetCore.Http.IFormFile formFile)
        {
            if (formFile == null) throw new ArgumentNullException(nameof(formFile));
            await using var stream = formFile.OpenReadStream();
            return await UploadFileAsync(stream, formFile.FileName, formFile.ContentType);
        }

        /// <summary>
        /// Download file -> trả về byte[]
        /// </summary>
        public async Task<byte[]> DownloadFileAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName required", nameof(fileName));

            var bucket = _supabaseClient.Storage.From(_bucketName);

            // gọi rõ overload: (string, TransformOptions?, EventHandler<float>?)
            var bytes = await bucket.Download(fileName, (StorageTransformOptions?)null, null);
            return bytes;
        }

        /// <summary>
        /// List file objects
        /// </summary>
        public async Task<List<FileDto>> ListFilesAsync(string? path = null)
        {
            var bucket = _supabaseClient.Storage.From(_bucketName);
            var files = await bucket.List(path ?? string.Empty);

            return files.Select(f => new FileDto
            {
                Name = f.Name,
                LastModified = f.UpdatedAt ?? DateTime.MinValue,
                BucketId = f.BucketId
            }).ToList();
        }

        /// <summary>
        /// Remove files
        /// </summary>
        public async Task RemoveFilesAsync(params string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0) return;
            var bucket = _supabaseClient.Storage.From(_bucketName);
            await bucket.Remove(fileNames.ToList());
        }

        /// <summary>
        /// Create signed URL
        /// </summary>
        public async Task<string> CreateSignedUrlAsync(string fileName, int expiresInSeconds = 3600)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName required", nameof(fileName));
            var bucket = _supabaseClient.Storage.From(_bucketName);
            return await bucket.CreateSignedUrl(fileName, expiresInSeconds);
        }

        /// <summary>
        /// Get public URL
        /// </summary>
        public string GetPublicUrl(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName required", nameof(fileName));
            var bucket = _supabaseClient.Storage.From(_bucketName);
            return bucket.GetPublicUrl(fileName);
        }
    }
}
