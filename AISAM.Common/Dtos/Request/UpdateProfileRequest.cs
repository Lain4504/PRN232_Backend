using System.ComponentModel.DataAnnotations;
using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Request
{
    public class UpdateProfileRequest
    {
        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(1000)]
        public string? Bio { get; set; }

        [MaxLength(500)]
        [Url]
        public string? AvatarUrl { get; set; }
    }
}