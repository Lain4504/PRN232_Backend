using AISAM.Data.Enumeration;

namespace AISAM.Common.Models
{
    public class PostListItemDto
    {
        public Guid Id { get; set; }
        public Guid ContentId { get; set; }
        public Guid IntegrationId { get; set; }
        public string? ExternalPostId { get; set; }
        public DateTime PublishedAt { get; set; }
        public ContentStatusEnum Status { get; set; }
        public bool IsDeleted { get; set; }
        public string? Link { get; set; }
    }
}