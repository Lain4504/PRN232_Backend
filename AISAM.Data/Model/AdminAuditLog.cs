using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model
{
    public class AdminAuditLog
    {
        [Key]
        public Guid Id { get; set; }
        
        [ForeignKey("User")]
        public Guid? AdminUserId { get; set; }
        
        [MaxLength(200)]
        public string Action { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? TargetType { get; set; }
        
        public Guid? TargetId { get; set; }
        
        public string? Details { get; set; } // JSONB as string
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User? AdminUser { get; set; }
    }
}
