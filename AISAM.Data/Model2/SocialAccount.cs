using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model2
{
    [Table("social_accounts")]
    public class SocialAccount
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("platform")]
        public SocialPlatformEnum Platform { get; set; }

        [MaxLength(255)]
        [Column("account_id")]
        public string? AccountId { get; set; } // ID từ mạng xã hội (e.g., Facebook user ID)

        [Required]
        [Column("user_access_token")]
        public string UserAccessToken { get; set; } = string.Empty; // Lưu user access token (encrypted)

        [Column("refresh_token")]
        public string? RefreshToken { get; set; } // Lưu refresh token (TikTok/Twitter, encrypted)

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; } // Hết hạn của user access token

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<SocialIntegration> SocialIntegrations { get; set; } = new List<SocialIntegration>();
    }
}
