using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreateAdCreativeFromContentRequest
    {
        [Required]
        public Guid ContentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string AdAccountId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CallToAction { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string LinkUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AdName { get; set; }
    }
}
