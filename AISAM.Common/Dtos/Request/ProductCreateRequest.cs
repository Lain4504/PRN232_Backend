using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class ProductCreateRequest
    {
        [Required]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public decimal? Price { get; set; }

        // Các file ảnh upload kèm theo
        public List<IFormFile>? ImageFiles { get; set; }

        // Trường hợp client đã có URL ảnh (không cần upload file)
        public List<string>? ImageUrls { get; set; }
    }
}
