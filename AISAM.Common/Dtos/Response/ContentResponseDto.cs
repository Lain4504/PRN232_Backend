namespace AISAM.Common.Dtos.Response
{
    public class ContentResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public Guid BrandId { get; set; }
        public string? BrandName { get; set; }
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
        public string? ExternalPostId { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}


