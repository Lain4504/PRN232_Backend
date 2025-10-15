namespace AISAM.Common.Dtos.Response
{
    public class CampaignResponse
    {
        public Guid Id { get; set; }
        public Guid BrandId { get; set; }
        public string AdAccountId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Objective { get; set; }
        public decimal? Budget { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


