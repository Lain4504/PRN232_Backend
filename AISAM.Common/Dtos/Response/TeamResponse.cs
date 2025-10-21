namespace AISAM.Common.Dtos.Response
{
    public class TeamResponse
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string VendorEmail { get; set; } = string.Empty;
    }
}