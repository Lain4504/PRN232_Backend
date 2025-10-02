using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Request
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
        public Guid? IntegrationId { get; set; }
    }
}


