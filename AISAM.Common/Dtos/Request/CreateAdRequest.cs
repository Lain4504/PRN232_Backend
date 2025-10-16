using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreateAdRequest
    {
        [Required]
        public Guid AdSetId { get; set; }

        [Required]
        public Guid CreativeId { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "PAUSED";
    }
}
