using Microsoft.AspNetCore.Http;
using System.ComponentModel;

namespace AISAM.Common.Dtos.Request
{
    public class ProductUpdateRequestDto
    {
        public Guid? BrandId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public List<IFormFile>? ImageFiles { get; set; } 
    }
}
