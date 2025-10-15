using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreateCampaignRequest
    {
        [Required]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Objective { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Budget { get; set; }
    }
}


