using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model
{
    [Table("team_members")]
    public class TeamMember
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("team_id")]
        public Guid TeamId { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("role")]
        public string Role { get; set; } = string.Empty;

        [Column("permissions", TypeName = "jsonb")]
        public string Permissions { get; set; } = "{}"; // JSON permissions

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("TeamId")]
        public virtual Team Team { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
