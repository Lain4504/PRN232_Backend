using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using ContentEntity = AISAM.Data.Model.Content;

namespace AISAM.Services.Service
{
    public class AIService : IAIService
    {
        private readonly GeminiSettings _settings;
        private readonly ILogger<AIService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IContentRepository _contentRepository;
        private readonly IAiGenerationRepository _aiGenerationRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IUserRepository _userRepository;
        private readonly Dictionary<string, IProviderService> _providers;

        public AIService(
            IOptions<GeminiSettings> settings,
            ILogger<AIService> logger,
            HttpClient httpClient,
            IContentRepository contentRepository,
            IAiGenerationRepository aiGenerationRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IUserRepository userRepository,
            IEnumerable<IProviderService> providers)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClient = httpClient;
            _contentRepository = contentRepository;
            _aiGenerationRepository = aiGenerationRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _userRepository = userRepository;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p);

            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                throw new ArgumentException("Gemini API key is not configured");
            }
        }

        public async Task<AiGenerationResponse> GenerateContentForDraftAsync(CreateDraftRequest request)
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Create draft content
            var content = new ContentEntity
            {
                BrandId = request.BrandId,
                ProductId = request.ProductId,
                AdType = request.AdType,
                Title = request.Title,
                TextContent = "", // Will be filled by AI
                ImageUrl = request.ImageUrl,
                VideoUrl = request.VideoUrl,
                Status = ContentStatusEnum.Draft
            };

            await _contentRepository.CreateAsync(content);

            // Create AI generation record
            var aiGeneration = new AiGeneration
            {
                ContentId = content.Id,
                AiPrompt = request.AIGenerationPrompt,
                Status = AiStatusEnum.Pending
            };

            await _aiGenerationRepository.CreateAsync(aiGeneration);

            try
            {
                // Generate content using Gemini
                var generatedText = await GenerateContentWithGemini(request.AIGenerationPrompt);

                // Update AI generation with results
                aiGeneration.GeneratedText = generatedText;
                aiGeneration.Status = AiStatusEnum.Completed;

                await _aiGenerationRepository.UpdateAsync(aiGeneration);

                return new AiGenerationResponse
                {
                    AiGenerationId = aiGeneration.Id,
                    ContentId = content.Id,
                    GeneratedText = generatedText,
                    Status = AiStatusEnum.Completed,
                    CreatedAt = aiGeneration.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI content for draft {ContentId}", content.Id);

                // Update AI generation with error
                aiGeneration.Status = AiStatusEnum.Failed;
                aiGeneration.ErrorMessage = ex.Message;
                await _aiGenerationRepository.UpdateAsync(aiGeneration);

                return new AiGenerationResponse
                {
                    AiGenerationId = aiGeneration.Id,
                    ContentId = content.Id,
                    Status = AiStatusEnum.Failed,
                    ErrorMessage = ex.Message,
                    CreatedAt = aiGeneration.CreatedAt
                };
            }
        }

        public async Task<AiGenerationResponse> ImproveContentAsync(Guid contentId, string improvementPrompt)
        {
            // Get existing content
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Create AI generation record for improvement
            var aiGeneration = new AiGeneration
            {
                ContentId = contentId,
                AiPrompt = improvementPrompt,
                Status = AiStatusEnum.Pending
            };

            await _aiGenerationRepository.CreateAsync(aiGeneration);

            try
            {
                // Generate improved content
                var improvedText = await GenerateContentWithGemini(improvementPrompt);

                // Update AI generation with results
                aiGeneration.GeneratedText = improvedText;
                aiGeneration.Status = AiStatusEnum.Completed;

                await _aiGenerationRepository.UpdateAsync(aiGeneration);

                return new AiGenerationResponse
                {
                    AiGenerationId = aiGeneration.Id,
                    ContentId = contentId,
                    GeneratedText = improvedText,
                    Status = AiStatusEnum.Completed,
                    CreatedAt = aiGeneration.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to improve content {ContentId}", contentId);

                // Update AI generation with error
                aiGeneration.Status = AiStatusEnum.Failed;
                aiGeneration.ErrorMessage = ex.Message;
                await _aiGenerationRepository.UpdateAsync(aiGeneration);

                return new AiGenerationResponse
                {
                    AiGenerationId = aiGeneration.Id,
                    ContentId = contentId,
                    Status = AiStatusEnum.Failed,
                    ErrorMessage = ex.Message,
                    CreatedAt = aiGeneration.CreatedAt
                };
            }
        }

        public async Task<ContentResponseDto> ApproveAIGenerationAsync(Guid aiGenerationId)
        {
            // Get AI generation
            var aiGeneration = await _aiGenerationRepository.GetByIdAsync(aiGenerationId);
            if (aiGeneration == null)
            {
                throw new ArgumentException("AI generation not found");
            }

            if (aiGeneration.Status != AiStatusEnum.Completed || string.IsNullOrEmpty(aiGeneration.GeneratedText))
            {
                throw new InvalidOperationException("AI generation is not completed or has no content");
            }

            // Get the associated content
            var content = await _contentRepository.GetByIdAsync(aiGeneration.ContentId);
            if (content == null)
            {
                throw new ArgumentException("Associated content not found");
            }

            // Update content with AI-generated text
            content.TextContent = aiGeneration.GeneratedText;
            content.Status = ContentStatusEnum.Approved; // Mark as approved
            content.UpdatedAt = DateTime.UtcNow;

            await _contentRepository.UpdateAsync(content);

            _logger.LogInformation("AI generation {AiGenerationId} approved and copied to content {ContentId}",
                aiGenerationId, content.Id);

            return MapToContentDto(content, null);
        }

        public async Task<IEnumerable<AiGenerationDto>> GetContentAIGenerationsAsync(Guid contentId)
        {
            var aiGenerations = await _aiGenerationRepository.GetByContentIdAsync(contentId);

            return aiGenerations.Select(ag => new AiGenerationDto
            {
                Id = ag.Id,
                AiPrompt = ag.AiPrompt,
                GeneratedText = ag.GeneratedText,
                GeneratedImageUrl = ag.GeneratedImageUrl,
                GeneratedVideoUrl = ag.GeneratedVideoUrl,
                Status = ag.Status,
                ErrorMessage = ag.ErrorMessage,
                CreatedAt = ag.CreatedAt,
                UpdatedAt = ag.UpdatedAt
            });
        }

        private async Task<string> GenerateContentWithGemini(string prompt)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        maxOutputTokens = Math.Min(1000, _settings.MaxTokens),
                        temperature = _settings.Temperature
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>();
                    if (result?.candidates?.Length > 0 && result.candidates[0].content?.parts?.Length > 0)
                    {
                        return result.candidates[0].content.parts[0].text?.Trim() ?? "No content generated";
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new Exception($"Gemini API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content with Gemini");
                throw;
            }
        }

        // Helper classes for Gemini API response
        private class GeminiApiResponse
        {
            public Candidate[]? candidates { get; set; }
            public UsageMetadata? usageMetadata { get; set; }
        }

        private class Candidate
        {
            public Content? content { get; set; }
        }

        private class Content
        {
            public Part[]? parts { get; set; }
        }

        private class Part
        {
            public string? text { get; set; }
        }

        private class UsageMetadata
        {
            public int totalTokenCount { get; set; }
        }

        private ContentResponseDto MapToContentDto(ContentEntity content, PublishResultDto? publishResult)
        {
            return new ContentResponseDto
            {
                Id = content.Id,
                BrandId = content.BrandId,
                ProductId = content.ProductId,
                AdType = content.AdType.ToString(),
                Title = content.Title,
                TextContent = content.TextContent,
                ImageUrl = content.ImageUrl,
                VideoUrl = content.VideoUrl,
                Status = content.Status.ToString(),
                CreatedAt = content.CreatedAt,
                UpdatedAt = content.UpdatedAt,
                ExternalPostId = publishResult?.ProviderPostId,
                PublishedAt = publishResult?.PostedAt
            };
        }
    }
}