using Microsoft.AspNetCore.Mvc;
using BookStore.Services.IServices;
using BookStore.Common;
using System.Net;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IImageUploadService _imageUploadService;

        public ImageController(IImageUploadService imageUploadService)
        {
            _imageUploadService = imageUploadService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(IFormFile file, string folder = "posts")
        {
            try
            {
                var imageUrl = await _imageUploadService.UploadImageAsync(file, folder);

                var response = GenericResponse<object>.CreateSuccess(
                    new { ImageUrl = imageUrl },
                    "Image uploaded successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = GenericResponse<object>.CreateError(
                    ex.Message,
                    HttpStatusCode.BadRequest,
                    "UPLOAD_ERROR"
                );

                return BadRequest(response);
            }
        }
    }
}