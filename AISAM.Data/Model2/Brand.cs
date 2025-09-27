using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model2
{
    [Table("brands")]
    public class Brand
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [MaxLength(500)]
        [Column("logo_url")]
        public string? LogoUrl { get; set; }

        [MaxLength(255)]
        [Column("slogan")]
        public string? Slogan { get; set; }

        [Column("usp")]
        public string? Usp { get; set; } // Unique Selling Points

        [Column("target_audience")]
        public string? TargetAudience { get; set; }

        [Column("profile_id")]
        public Guid? ProfileId { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ProfileId")]
        public virtual Profile? Profile { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Content> Contents { get; set; } = new List<Content>();
        public virtual ICollection<SocialIntegration> SocialIntegrations { get; set; } = new List<SocialIntegration>();
        public virtual ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
    }
}
