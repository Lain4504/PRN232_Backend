using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreateCreativeRequest
    {
        [Required]
        public Guid ContentId { get; set; }

        [Required]
        public Guid BrandId { get; set; }

        [MaxLength(50)]
        public string? CallToAction { get; set; }
    }
}


