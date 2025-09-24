using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model
{
    public class SocialTarget
    {
        [Key]
        public Guid Id { get; set; }
        
        [ForeignKey("SocialAccount")]
        public Guid SocialAccountId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string ProviderTargetId { get; set; } = string.Empty; // Page ID, Profile ID, Channel ID
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty; // Page name, Profile name, Channel name
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // 'page', 'profile', 'business_account', 'channel'
        
        public string? AccessToken { get; set; } // Some providers need separate page/target tokens
        
        public string? Category { get; set; }
        
        public string? ProfilePictureUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string? Metadata { get; set; } // JSON for provider-specific data
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual SocialAccount SocialAccount { get; set; } = null!;
        public virtual ICollection<SocialPost> Posts { get; set; } = new List<SocialPost>();
    }
}
