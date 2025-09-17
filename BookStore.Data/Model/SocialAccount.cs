using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Data.Model
{
    public class SocialAccount
    {
        [Key]
        public int Id { get; set; }
        
        [ForeignKey("User")]
        public int UserId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty; // 'facebook', 'instagram', 'tiktok'
        
        [Required]
        [MaxLength(255)]
        public string ProviderUserId { get; set; } = string.Empty; // User ID at the provider
        
        [Required]
        public string AccessToken { get; set; } = string.Empty; // Should be encrypted in production
        
        public string? RefreshToken { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        public string? Scope { get; set; } // Permissions granted
        
        public bool IsActive { get; set; } = true;
        
        public string? Metadata { get; set; } // JSON for provider-specific data
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<SocialTarget> SocialTargets { get; set; } = new List<SocialTarget>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
