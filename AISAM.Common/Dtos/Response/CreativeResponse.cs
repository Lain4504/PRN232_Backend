namespace AISAM.Common.Dtos.Response
{
    public class CreativeResponse
    {
        public Guid Id { get; set; }
        public Guid ContentId { get; set; }
        public string AdAccountId { get; set; } = string.Empty;
        public string? CreativeExternalId { get; set; }
        public string? CallToAction { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


