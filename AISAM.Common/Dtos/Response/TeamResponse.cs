using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class TeamResponse
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Active";
        public int MembersCount { get; set; } = 0;
        public string? UserRole { get; set; }
    }
}