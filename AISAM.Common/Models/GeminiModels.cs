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

    // AI Workflow DTOs
    public class CreateDraftRequest
    {
        public Guid UserId { get; set; }
        public Guid BrandId { get; set; }
        public Guid? ProductId { get; set; }
        public AdTypeEnum AdType { get; set; }
        public string? Title { get; set; }
        public string AIGenerationPrompt { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
    }

    public class AiGenerationResponse
    {
        public Guid AiGenerationId { get; set; }
        public Guid ContentId { get; set; }
        public string? GeneratedText { get; set; }
        public string? GeneratedImageUrl { get; set; }
        public string? GeneratedVideoUrl { get; set; }
        public AiStatusEnum Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AiGenerationDto
    {
        public Guid Id { get; set; }
        public string AiPrompt { get; set; } = string.Empty;
        public string? GeneratedText { get; set; }
        public string? GeneratedImageUrl { get; set; }
        public string? GeneratedVideoUrl { get; set; }
        public AiStatusEnum Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ApproveAIGenerationRequest
    {
        public Guid AiGenerationId { get; set; }
        public bool PublishImmediately { get; set; } = false;
        public Guid? IntegrationId { get; set; }
    }

    public class ImproveContentRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}