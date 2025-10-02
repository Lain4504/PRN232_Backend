using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface IAIService
    {
        /// <summary>
        /// Generate content using AI based on a prompt
        /// </summary>
        Task<string> GenerateContentAsync(string prompt);

        /// <summary>
        /// Improve existing content
        /// </summary>
        Task<string> ImproveContentAsync(string content);

        /// <summary>
        /// Save approved AI-generated content to database
        /// </summary>
        Task<ContentResponseDto> SaveAIContentAsync(AISaveContentRequest request);
    }
}