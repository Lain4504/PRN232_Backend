using Microsoft.AspNetCore.Http;

namespace BookStore.Services.IServices
{
    public interface IImageUploadService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "posts");
        Task<bool> DeleteImageAsync(string publicId);       
    }
}