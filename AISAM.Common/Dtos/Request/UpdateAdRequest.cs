using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class UpdateAdRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty; // active | paused
    }
}


