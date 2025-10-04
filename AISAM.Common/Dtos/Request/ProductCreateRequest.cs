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

        [MaxLength(2000)]
        public string? Description { get; set; }

        public decimal? Price { get; set; }

        [Required]
        public List<IFormFile>? ImageFiles { get; set; }
    }
}
