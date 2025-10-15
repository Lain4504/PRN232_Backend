using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreateAdSetRequest
    {
        [Required]
        public Guid CampaignId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? TargetingJson { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? DailyBudget { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}


