using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }
        
        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // App-managed role separate from Supabase Auth; e.g., "user", "staff", "admin"
        public string Role { get; set; } = "user";

        // Navigation properties
        public virtual ICollection<SocialAccount> SocialAccounts { get; set; } = new List<SocialAccount>();
        public virtual IEnumerable<SocialPost>? Posts { get; set; } = new List<SocialPost>();
    }
}
