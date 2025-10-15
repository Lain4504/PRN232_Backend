namespace AISAM.Common.Dtos.Response
{
    public class AdResponse
    {
        public Guid Id { get; set; }
        public Guid AdSetId { get; set; }
        public Guid CreativeId { get; set; }
        public string? ExternalAdId { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


