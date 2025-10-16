namespace AISAM.Common.Dtos.Response
{
    public class AdPerformanceResponse
    {
        public Guid AdId { get; set; }
        public string AdName { get; set; } = string.Empty;
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public long Engagement { get; set; }
        public decimal Ctr { get; set; }
        public decimal Spend { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public DateTime ReportDate { get; set; }
        public string? RawData { get; set; } // JSON data from Facebook API
    }
}
