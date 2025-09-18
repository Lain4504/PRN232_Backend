using System.ComponentModel.DataAnnotations;

namespace BookStore.Common.Models
{
    // Provider-agnostic DTOs
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

    public class SocialAccountDto
    {
        public int Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderUserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SocialTargetDto> Targets { get; set; } = new();
    }

    public class SocialTargetDto
    {
        public int Id { get; set; }
        public string ProviderTargetId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SocialAccountDto> SocialAccounts { get; set; } = new();
    }

    // Provider-specific DTOs (for backward compatibility and provider-specific operations)
    public class LinkSocialAccountRequest
    {
        public int UserId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? State { get; set; }
    }

    public class LinkPageByTokenRequest
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string PageAccessToken { get; set; } = string.Empty;
        
        public string? UserAccessToken { get; set; }
    }

    public class AuthUrlResponse
    {
        public string AuthUrl { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    // Authentication DTOs
    public class RegisterUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginUserDto
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<SocialAccountDto> SocialAccounts { get; set; } = new();
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponseDto User { get; set; } = new();
    }
}