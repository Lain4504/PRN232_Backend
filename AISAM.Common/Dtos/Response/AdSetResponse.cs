using AISAM.Common.Dtos.Response;

namespace AISAM.Common.Dtos.Response
{
    public class AdSetResponse
    {
        public Guid Id { get; set; }
        public Guid CampaignId { get; set; }
        public string? FacebookAdSetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Targeting { get; set; }
        public decimal? DailyBudget { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public AdSetMetrics? Metrics { get; set; }
        public List<AdResponse> Ads { get; set; } = new();
    }

    public class AdSetMetrics
    {
        public long TotalImpressions { get; set; }
        public long TotalClicks { get; set; }
        public decimal TotalSpend { get; set; }
        public decimal Ctr { get; set; }
        public int ActiveAds { get; set; }
        public int PausedAds { get; set; }
    }
}
