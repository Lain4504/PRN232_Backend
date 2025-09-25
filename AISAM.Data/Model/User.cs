using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }
        
        public string? Email { get; set; }
        
        public string? PasswordHash { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool isBanned { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        public bool MfaEnabled { get; set; } = false;
        public string Role { get; set; } = "user";
        
        // Navigation properties
        public virtual ICollection<SocialAccount> SocialAccounts { get; set; } = new List<SocialAccount>();
        public virtual IEnumerable<SocialPost>? Posts { get; set; } = new List<SocialPost>();
    }
}
