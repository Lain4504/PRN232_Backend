using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("social_integrations")]
    public class SocialIntegration
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("profile_id")]
        public Guid ProfileId { get; set; }

        [Required]
        [Column("brand_id")]
        public Guid BrandId { get; set; }

        [Required]
        [Column("social_account_id")]
        public Guid SocialAccountId { get; set; } // Liên kết tài khoản mạng xã hội

        [Required]
        [Column("platform")]
        public SocialPlatformEnum Platform { get; set; }

        [Required]
        [Column("access_token")]
        public string AccessToken { get; set; } = string.Empty; // Page access token (Facebook) hoặc access token (TikTok/Twitter)

        [Column("refresh_token")]
        public string? RefreshToken { get; set; } // Refresh token cho TikTok/Twitter

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [MaxLength(255)]
        [Column("external_id")]
        public string? ExternalId { get; set; } // Page ID (Facebook) hoặc account ID (TikTok/Twitter)

        [MaxLength(255)]
        [Column("ad_account_id")]
        public string? AdAccountId { get; set; } // Facebook Ad Account ID for Marketing API

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProfileId")]
        public virtual Profile Profile { get; set; } = null!;

        [ForeignKey("BrandId")]
        public virtual Brand Brand { get; set; } = null!;

        [ForeignKey("SocialAccountId")]
        public virtual SocialAccount SocialAccount { get; set; } = null!;

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
