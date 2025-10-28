namespace AISAM.Common.Dtos.Response
{
    public class TeamMemberResponseDto
    {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public bool CanApproveContent { get; set; }
    }
}
