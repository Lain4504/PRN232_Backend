using AISAM.Common.Dtos;
using AISAM.Data.Enumeration;
using AISAM.Services.Extensions;
using SupabaseClient = Supabase.Client;
using StorageFileOptions = Supabase.Storage.FileOptions;
using StorageTransformOptions = Supabase.Storage.TransformOptions;

namespace AISAM.Services.Service
{
    public class SupabaseStorageService
    {
        private readonly SupabaseClient _supabaseClient;

        public SupabaseStorageService(SupabaseClient supabaseClient)
        {
            _supabaseClient = supabaseClient ?? throw new ArgumentNullException(nameof(supabaseClient));
        }

        /// <summary>
        /// Các loại content type ảnh được hỗ trợ.
        /// </summary>
        private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp"
        };

        private const long MaxImageSize = 5 * 1024 * 1024; // 5MB

        /// <summary>
        /// Validate ảnh trước khi upload.
        /// </summary>
        private static void ValidateImageFile(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            if (file.Length > MaxImageSize)
                throw new InvalidOperationException("Ảnh không được lớn hơn 5MB");

            if (!AllowedImageTypes.Contains(file.ContentType))
                throw new InvalidOperationException("Chỉ hỗ trợ định dạng ảnh jpg, jpeg, png, webp");
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
        /// Upload file (chỉ định bucket)
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string originalFileName, string contentType, DefaultBucketEnum bucket)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("originalFileName required", nameof(originalFileName));

            // đọc stream thành byte[]
            await using var ms = new MemoryStream();
            if (fileStream.CanSeek)
                fileStream.Position = 0;
            await fileStream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var safeFileName = GenerateUniqueFileName(originalFileName);

            var bucketClient = _supabaseClient.Storage.From(bucket.GetName());

            await bucketClient.Upload(bytes, safeFileName, new StorageFileOptions
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                Upsert = false
            });

            return safeFileName;
        }

        /// <summary>
        /// Upload từ IFormFile (chỉ định bucket) + ✅ validation ảnh
        /// </summary>
        public async Task<string> UploadFileAsync(Microsoft.AspNetCore.Http.IFormFile formFile, DefaultBucketEnum bucket)
        {
            if (formFile == null) throw new ArgumentNullException(nameof(formFile));

            // ✅ Validate ảnh trước khi upload
            ValidateImageFile(formFile);

            await using var stream = formFile.OpenReadStream();
            return await UploadFileAsync(stream, formFile.FileName, formFile.ContentType, bucket);
        }

        /// <summary>
        /// Download file (chỉ định bucket)
        /// </summary>
        public async Task<byte[]> DownloadFileAsync(string fileName, DefaultBucketEnum bucket)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName required", nameof(fileName));

            var bucketClient = _supabaseClient.Storage.From(bucket.GetName());
            return await bucketClient.Download(fileName, (StorageTransformOptions?)null, null);
        }

        /// <summary>
        /// List file objects (chỉ định bucket)
        /// </summary>
        public async Task<List<FileDto>> ListFilesAsync(DefaultBucketEnum bucket, string? path = null)
        {
            var bucketClient = _supabaseClient.Storage.From(bucket.GetName());
            var files = await bucketClient.List(path ?? string.Empty);

            return files.Select(f => new FileDto
            {
                Name = f.Name,
                LastModified = f.UpdatedAt ?? DateTime.MinValue,
                BucketId = f.BucketId
            }).ToList();
        }

        /// <summary>
        /// Remove files (chỉ định bucket)
        /// </summary>
        public async Task RemoveFilesAsync(DefaultBucketEnum bucket, params string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0) return;
            var bucketClient = _supabaseClient.Storage.From(bucket.GetName());
            await bucketClient.Remove(fileNames.ToList());
        }

        /// <summary>
        /// Create signed URL (chỉ định bucket)
        /// </summary>
        public async Task<string> CreateSignedUrlAsync(string fileName, DefaultBucketEnum bucket, int expiresInSeconds = 3600)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName required", nameof(fileName));
            var bucketClient = _supabaseClient.Storage.From(bucket.GetName());
            return await bucketClient.CreateSignedUrl(fileName, expiresInSeconds);
        }

        /// <summary>
        /// Get public URL (chỉ định bucket)
        /// </summary>
        public string GetPublicUrl(string fileName, DefaultBucketEnum bucket)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName required", nameof(fileName));
            var bucketClient = _supabaseClient.Storage.From(bucket.GetName());
            return bucketClient.GetPublicUrl(fileName);
        }
    }
}