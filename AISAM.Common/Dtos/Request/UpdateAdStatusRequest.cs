using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class UpdateAdStatusRequest
    {
        [Required]
        public Guid AdId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // ACTIVE or PAUSED
    }
}
