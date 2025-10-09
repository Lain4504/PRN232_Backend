namespace AISAM.Common.Models
{
    public class PostDto
    {
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public string? ImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; }
        public string? VideoUrl { get; set; }
        public string? Metadata { get; set; }
    }
}