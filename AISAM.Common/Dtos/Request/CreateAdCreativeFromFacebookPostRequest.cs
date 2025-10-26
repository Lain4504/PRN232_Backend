using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreateAdCreativeFromFacebookPostRequest
    {
        [Required]
        [MaxLength(255)]
        public string AdAccountId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FacebookPostId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? CallToAction { get; set; }

        [MaxLength(500)]
        public string? LinkUrl { get; set; }

        [MaxLength(255)]
        public string? AdName { get; set; }

        [Required]
        public Guid BrandId { get; set; }
    }
}
