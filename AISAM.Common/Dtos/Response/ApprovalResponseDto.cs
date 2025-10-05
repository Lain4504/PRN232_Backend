namespace AISAM.Common.Dtos.Response
{
    public class ApprovalResponseDto
    {
        public Guid Id { get; set; }
        public Guid ContentId { get; set; }
        public Guid ApproverId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public ContentResponseDto? Content { get; set; }
        public UserResponseDto? Approver { get; set; }
    }
}