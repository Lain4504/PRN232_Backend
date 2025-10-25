using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class SubscriptionResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public SubscriptionPlanEnum Plan { get; set; }
        public int QuotaPostsPerMonth { get; set; }
        public int QuotaStorageGb { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? StripeCustomerId { get; set; }
    }
}