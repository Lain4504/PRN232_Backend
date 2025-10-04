using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface IAIService
    {
        /// <summary>
        /// Create a draft content and generate AI content for it
        /// </summary>
        Task<AiGenerationResponse> GenerateContentForDraftAsync(CreateDraftRequest request);

        /// <summary>
        /// Improve existing content and save as new AI generation
        /// </summary>
        Task<AiGenerationResponse> ImproveContentAsync(Guid contentId, string improvementPrompt);

        /// <summary>
        /// Approve AI generation and copy it to the content
        /// </summary>
        Task<ContentResponseDto> ApproveAIGenerationAsync(Guid aiGenerationId);

        /// <summary>
        /// Get all AI generations for a content
        /// </summary>
        Task<IEnumerable<AiGenerationDto>> GetContentAIGenerationsAsync(Guid contentId);
    }
}