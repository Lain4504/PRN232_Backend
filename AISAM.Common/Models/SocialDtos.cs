namespace AISAM.Common.Models
{
    public class SocialAccountDto
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderUserId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SocialTargetDto> Targets { get; set; } = new();
    }

    public class SocialTargetDto
    {
        public Guid Id { get; set; }
        public string ProviderTargetId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class LinkSocialAccountRequest
    {
        public Guid UserId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? State { get; set; }
    }
}


