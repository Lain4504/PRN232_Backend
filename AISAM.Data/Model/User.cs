using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        public string? Email { get; set; }
        
        public string? Username { get; set; }
        
        public string? PasswordHash { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<SocialAccount> SocialAccounts { get; set; } = new List<SocialAccount>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
