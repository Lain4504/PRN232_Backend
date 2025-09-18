using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Data.Model
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        
        [ForeignKey("User")]
        public int UserId { get; set; }
        
        [ForeignKey("SocialAccount")]
        public int? SocialAccountId { get; set; }
        
        [ForeignKey("SocialTarget")]
        public int? SocialTargetId { get; set; }
        
        [MaxLength(50)]
        public string? Provider { get; set; } // 'facebook', 'instagram', 'tiktok' - denormalized for quick queries
        
        [MaxLength(255)]
        public string? ProviderPostId { get; set; } // Post ID returned by provider after publish
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? LinkUrl { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public DateTime? ScheduledTime { get; set; }
        
        public DateTime? PostedAt { get; set; }
        
        [Required]
        public PostStatus Status { get; set; } = PostStatus.Draft;
        
        public string? ErrorMessage { get; set; }
        
        public string? Metadata { get; set; } // JSON for provider-specific post data
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual SocialAccount? SocialAccount { get; set; }
        public virtual SocialTarget? SocialTarget { get; set; }
    }
    
    public enum PostStatus
    {
        Draft = 0,
        Scheduled = 1,
        Posted = 2,
        Failed = 3,
        Cancelled = 4
    }
}