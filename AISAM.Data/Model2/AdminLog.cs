using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model2
{
    [Table("admin_logs")]
    public class AdminLog
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("admin_id")]
        public Guid AdminId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("action_type")]
        public string ActionType { get; set; } = string.Empty;

        [Column("target_id")]
        public Guid? TargetId { get; set; }

        [MaxLength(50)]
        [Column("target_type")]
        public string? TargetType { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("AdminId")]
        public virtual User Admin { get; set; } = null!;
    }
}
