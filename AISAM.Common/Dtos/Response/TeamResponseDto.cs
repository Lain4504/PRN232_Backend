namespace AISAM.Common.Dtos.Response
{
    public class TeamResponseDto
    {
        public Guid Id { get; set; }
        public Guid VendorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
