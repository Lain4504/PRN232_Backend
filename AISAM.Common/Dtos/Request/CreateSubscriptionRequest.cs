using System.ComponentModel.DataAnnotations;
using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Request
{
    public class CreateSubscriptionRequest
    {
        [Required]
        public Guid ProfileId { get; set; }

        [Required]
        public SubscriptionPlanEnum Plan { get; set; }



        public bool IsRecurring { get; set; } = true; // Monthly recurring subscription
    }
}