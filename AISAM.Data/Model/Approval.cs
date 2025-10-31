using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("approvals")]
    public class Approval
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("content_id")]
        public Guid ContentId { get; set; }

        // Legacy: kept for backward compatibility until migration cleanup
        [Column("approver_profile_id")]
        public Guid? ApproverProfileId { get; set; }

        // New: approver by userId (team member)
        [Required]
        [Column("approver_user_id")]
        public Guid ApproverUserId { get; set; }

        [Required]
        [Column("status")]
        public ContentStatusEnum Status { get; set; } = ContentStatusEnum.PendingApproval;

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ContentId")]
        public virtual Content Content { get; set; } = null!;

        [ForeignKey("ApproverProfileId")]
        public virtual Profile? ApproverProfile { get; set; }

        [ForeignKey("ApproverUserId")]
        public virtual User ApproverUser { get; set; } = null!;
    }
}
