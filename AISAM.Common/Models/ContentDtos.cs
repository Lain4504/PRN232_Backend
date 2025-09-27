
using AISAM.Data.Enumeration;

namespace AISAM.Common.Models
{
    public class CreateContentRequest
    {
        public Guid UserId { get; set; }
        public Guid BrandId { get; set; }
        public Guid? ProductId { get; set; }
        public AdTypeEnum AdType { get; set; }
        public string? Title { get; set; }
        public string TextContent { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? StyleDescription { get; set; }
        public string? ContextDescription { get; set; }
        public string? RepresentativeCharacter { get; set; }
        public bool PublishImmediately { get; set; } = false;
        public Guid? IntegrationId { get; set; } // SocialIntegration ID to publish to
    }

    public class ContentResponseDto
    {
        public Guid Id { get; set; }
        public Guid BrandId { get; set; }
        public Guid? ProductId { get; set; }
        public string AdType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string TextContent { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? StyleDescription { get; set; }
        public string? ContextDescription { get; set; }
        public string? RepresentativeCharacter { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? ExternalPostId { get; set; } // If published
        public DateTime? PublishedAt { get; set; } // If published
    }

    public class PublishContentRequest
    {
        public Guid ContentId { get; set; }
        public Guid IntegrationId { get; set; }
    }
}
