using AISAM.Common.Dtos.Response;

namespace AISAM.Common.Dtos.Response
{
    public class AdResponse
    {
        public Guid Id { get; set; }
        public Guid AdSetId { get; set; }
        public Guid CreativeId { get; set; }
        public string? AdId { get; set; }
        public string? Status { get; set; }
        public string? Name { get; set; }
        public string? AdSetName { get; set; }
        public string? PageName { get; set; }
        public DateTime CreatedAt { get; set; }
        public AdMetrics? Metrics { get; set; }
    }

    public class AdMetrics
    {
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public decimal Spend { get; set; }
        public decimal Ctr { get; set; }
        public long Engagement { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
