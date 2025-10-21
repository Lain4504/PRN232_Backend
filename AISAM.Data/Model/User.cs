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
        
        [Required]
        [Column("role")]
        public UserRoleEnum Role { get; set; } = UserRoleEnum.User;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Profile> Profiles { get; set; } = new List<Profile>();
        public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    }
}
