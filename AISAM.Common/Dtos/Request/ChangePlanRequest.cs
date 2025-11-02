using System.ComponentModel.DataAnnotations;
using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Request
{
    public class ChangePlanRequest
    {
        [Required]
        public SubscriptionPlanEnum PlanId { get; set; }

        [Required]
        public string BillingCycle { get; set; } = "monthly"; // "monthly" or "yearly"

        public bool Immediate { get; set; } = true; // If true, change immediately; if false, change at period end
    }
}

