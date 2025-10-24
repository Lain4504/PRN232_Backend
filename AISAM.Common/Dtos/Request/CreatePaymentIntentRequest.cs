using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreatePaymentIntentRequest
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "USD";

        [Required]
        public int SubscriptionPlanId { get; set; } // Maps to SubscriptionPlanEnum

        public string? Description { get; set; }
    }
}