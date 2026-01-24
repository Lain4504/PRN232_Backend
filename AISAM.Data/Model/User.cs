using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("full_name")]
        public string? FullName { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("password_salt")]
        public string PasswordSalt { get; set; } = string.Empty;
        
        [Required]
        [Column("role")]
        public UserRoleEnum Role { get; set; } = UserRoleEnum.User;

        [Column("is_email_verified")]
        public bool IsEmailVerified { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public virtual ICollection<Profile> Profiles { get; set; } = new List<Profile>();
        public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
