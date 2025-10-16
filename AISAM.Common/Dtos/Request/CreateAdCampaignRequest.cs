using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreateAdCampaignRequest
    {
        [Required]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(255)]
        public string AdAccountId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Objective { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Budget must be greater than 0")]
        public decimal Budget { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
