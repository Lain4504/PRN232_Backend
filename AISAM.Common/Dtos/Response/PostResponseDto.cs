using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class PostResponseDto
    {
        public Guid Id { get; set; }
        public Guid ContentId { get; set; }
        public Guid IntegrationId { get; set; }
        public string? ExternalPostId { get; set; }
        public DateTime PublishedAt { get; set; }
        public ContentStatusEnum Status { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties info
        public ContentInfoDto? Content { get; set; }
        public IntegrationInfoDto? Integration { get; set; }
    }

    public class ContentInfoDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string TextContent { get; set; } = string.Empty;
        public AdTypeEnum AdType { get; set; }
        public ContentStatusEnum Status { get; set; }
        public string? BrandName { get; set; }
    }

    public class IntegrationInfoDto
    {
        public Guid Id { get; set; }
        public string PlatformName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}