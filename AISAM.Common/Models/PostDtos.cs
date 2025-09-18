using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Models
{
    public class CreatePostRequest
    {
        public int UserId { get; set; }
        public int SocialAccountId { get; set; }
        public int SocialTargetId { get; set; }
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? LinkUrl { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public bool Published { get; set; } = false;
    }

    public class SchedulePostRequest
    {
        public int UserId { get; set; }
        public int SocialAccountId { get; set; }
        public int SocialTargetId { get; set; }
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? LinkUrl { get; set; }
        
        public string? ImageUrl { get; set; }
        
        [Required]
        public DateTime ScheduledTime { get; set; }
    }

    public class PostResponseDto
    {
        public int Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? ProviderPostId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public DateTime? PostedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


