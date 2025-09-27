using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model2
{
    [Table("profiles")]
    public class Profile
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("profile_type")]
        public ProfileTypeEnum ProfileType { get; set; }

        [MaxLength(255)]
        [Column("company_name")]
        public string? CompanyName { get; set; }

        [Column("bio")]
        public string? Bio { get; set; }

        [MaxLength(500)]
        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
    }
}
