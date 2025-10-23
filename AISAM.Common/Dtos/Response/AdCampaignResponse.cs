using AISAM.Common.Dtos.Response;

namespace AISAM.Common.Dtos.Response
{
    public class AdCampaignResponse
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public Guid BrandId { get; set; }
        public string AdAccountId { get; set; } = string.Empty;
        public string? FacebookCampaignId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Objective { get; set; }
        public decimal? Budget { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public AdCampaignMetrics? Metrics { get; set; }
        public List<AdSetResponse> AdSets { get; set; } = new();
    }

    public class AdCampaignMetrics
    {
        public long TotalImpressions { get; set; }
        public long TotalClicks { get; set; }
        public decimal TotalSpend { get; set; }
        public decimal Ctr { get; set; }
        public int ActiveAds { get; set; }
        public int PausedAds { get; set; }
    }
}
