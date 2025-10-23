namespace AISAM.Common.Dtos.Response
{
    public class AuditLogResponseDto
    {
        public Guid Id { get; set; }
        public Guid ActorId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string TargetTable { get; set; } = string.Empty;
        public Guid TargetId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}