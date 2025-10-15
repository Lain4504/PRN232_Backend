namespace AISAM.Common.Dtos.Response
{
    public class AdSetResponse
    {
        public Guid Id { get; set; }
        public Guid CampaignId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TargetingJson { get; set; }
        public decimal? DailyBudget { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


