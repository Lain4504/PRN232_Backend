using BookStore.Common.Settings;
using BookStore.Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Npgsql.BackendMessages;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace BookStore.Services.Service
{
    public class CloudinaryImageUploadService : IImageUploadService
    {

        private readonly Cloudinary _cloudinary;

        public CloudinaryImageUploadService(IOptions<CloudinarySettings> config)
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = "posts")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            // Check if the file is a valid image type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                throw new ArgumentException("Only image files are allowed");

            // Check file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024) // 5MB limit
                throw new ArgumentException("File size exceeds the 5MB limit");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation()
                    .Width(800)
                    .Height(600)
                    .Crop("limit")
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var uploadResult = _cloudinary.Upload(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Image upload failed: {uploadResult.Error.Message}");

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId);
            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);
            return deletionResult.Result == "ok";
        }
    }
}