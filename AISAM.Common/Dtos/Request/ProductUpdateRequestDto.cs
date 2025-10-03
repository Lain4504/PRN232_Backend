using Microsoft.AspNetCore.Http;

namespace AISAM.Common.Dtos.Request
{
    public class ProductUpdateRequestDto
    {
        public Guid? BrandId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public List<string>? Images { get; set; } // override trực tiếp URL nếu muốn

        // 👇 thêm vào để có thể upload ảnh khi update
        public List<IFormFile>? ImageFiles { get; set; }
    }
}
