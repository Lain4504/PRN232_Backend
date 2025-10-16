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

        [Required]
        public string Targeting { get; set; } = string.Empty; // JSON targeting configuration

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Daily budget must be greater than 0")]
        public decimal DailyBudget { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
