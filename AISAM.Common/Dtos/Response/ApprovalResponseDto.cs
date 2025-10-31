namespace AISAM.Common.Dtos.Response
{
    public class ApprovalResponseDto
    {
        public Guid Id { get; set; }
        public Guid ContentId { get; set; }
        public Guid? ApproverProfileId { get; set; }
        public Guid ApproverUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Flattened convenience properties for frontend
        public string? ContentTitle { get; set; }
        public string? BrandName { get; set; }
        public Guid? BrandId { get; set; }
        public string? ApproverEmail { get; set; }
        public string? ApproverName { get; set; }
        
        // Navigation properties (kept for backward compatibility)
        public ContentResponseDto? Content { get; set; }
        public ProfileResponseDto? ApproverProfile { get; set; }
    }
}