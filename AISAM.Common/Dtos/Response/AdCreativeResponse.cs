namespace AISAM.Common.Dtos.Response
{
    public class AdCreativeResponse
    {
        public Guid Id { get; set; }
        public Guid? ContentId { get; set; }
        public string AdAccountId { get; set; } = string.Empty;
        public string? CreativeId { get; set; }
        public string? CallToAction { get; set; }
        public string? FacebookPostId { get; set; }
        public DateTime CreatedAt { get; set; }
        public AdCreativePreview? ContentPreview { get; set; }
    }

    public class AdCreativePreview
    {
        public string? Title { get; set; }
        public string TextContent { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string AdType { get; set; } = string.Empty;
    }
}
