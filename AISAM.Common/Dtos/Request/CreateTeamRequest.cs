namespace AISAM.Common.DTOs.Request
{
    public class CreateTeamRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<Guid>? BrandIds { get; set; }
    }
}