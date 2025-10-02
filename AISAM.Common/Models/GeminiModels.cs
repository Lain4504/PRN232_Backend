using AISAM.Data.Enumeration;

namespace AISAM.Common.Models
{
    public class GeminiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-1.5-flash";
        public int MaxTokens { get; set; } = 2048;
        public double Temperature { get; set; } = 0.7;
    }

    public class AISaveContentRequest
    {
        public Guid UserId { get; set; }
        public Guid BrandId { get; set; }
        public Guid? ProductId { get; set; }
        public AdTypeEnum AdType { get; set; }
        public string? Title { get; set; }
        public string AIGeneratedContent { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public bool PublishImmediately { get; set; } = false;
        public Guid? IntegrationId { get; set; }
    }
}