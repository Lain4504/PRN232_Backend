using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreateAdCreativeRequest
    {
        [Required]
        public Guid ContentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string AdAccountId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? CallToAction { get; set; }
    }
}
