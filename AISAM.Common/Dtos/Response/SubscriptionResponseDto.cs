using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class SubscriptionResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public SubscriptionPlanEnum Plan { get; set; }
        public int QuotaPostsPerMonth { get; set; }
        public int QuotaAIContentPerDay { get; set; }
        public int QuotaAIImagesPerDay { get; set; }
        public int QuotaPlatforms { get; set; }
        public int QuotaAccounts { get; set; }
        public int AnalysisLevel { get; set; }
        public decimal QuotaAdBudgetMonthly { get; set; }
        public int QuotaAdCampaigns { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PayOSOrderCode { get; set; }
        public string? PayOSPaymentLinkId { get; set; }
    }
}