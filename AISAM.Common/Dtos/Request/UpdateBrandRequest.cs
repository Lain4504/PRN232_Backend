using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class UpdateBrandRequest
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(255)]
        public string? Slogan { get; set; }

        public string? Usp { get; set; }

        public string? TargetAudience { get; set; }

        public Guid? ProfileId { get; set; }
    }
}