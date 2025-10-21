using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos
{
    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public SubscriptionPlanEnum Plan { get; set; }
        public int QuotaPostsPerMonth { get; set; }
        public int QuotaStorageGb { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateSubscriptionRequest
    {
        public SubscriptionPlanEnum Plan { get; set; }
    }

    public class UpdateSubscriptionRequest
    {
        public SubscriptionPlanEnum Plan { get; set; }
    }

    public class PaymentOrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class PaymentWebhookRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "success", "failed", "refunded"
    }
}