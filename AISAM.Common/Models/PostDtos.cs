using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Models
{
    public class CreatePostRequest
    {
        public Guid UserId { get; set; }
        public Guid SocialAccountId { get; set; }
        public Guid SocialTargetId { get; set; }
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? LinkUrl { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public bool Published { get; set; } = false;
    }

    public class SchedulePostRequest
    {
        public Guid UserId { get; set; }
        public Guid SocialAccountId { get; set; }
        public Guid SocialTargetId { get; set; }
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? LinkUrl { get; set; }
        
        public string? ImageUrl { get; set; }
        
        [Required]
        public DateTime ScheduledTime { get; set; }
    }

    public class PostResponseDto
    {
        public Guid Id { get; set; }
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


