using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace AISAM.Services.Service
{
    public class AIService : IAIService
    {
        private readonly GeminiSettings _settings;
        private readonly ILogger<AIService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IContentRepository _contentRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IUserRepository _userRepository;
        private readonly Dictionary<string, IProviderService> _providers;

        public AIService(
            IOptions<GeminiSettings> settings,
            ILogger<AIService> logger,
            HttpClient httpClient,
            IContentRepository contentRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IUserRepository userRepository,
            IEnumerable<IProviderService> providers)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClient = httpClient;
            _contentRepository = contentRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _userRepository = userRepository;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p);

            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                throw new ArgumentException("Gemini API key is not configured");
            }
        }

        public async Task<string> GenerateContentAsync(string prompt)
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
                return $"Error: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content with AI");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> ImproveContentAsync(string content)
        {
            var prompt = $"Improve and enhance the following content. Make it more engaging and professional:\n\n{content}";
            return await GenerateContentAsync(prompt);
        }

        public async Task<ContentResponseDto> SaveAIContentAsync(AISaveContentRequest request)
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Create content entity with AI-generated text
            var content = new AISAM.Data.Model.Content
            {
                BrandId = request.BrandId,
                ProductId = request.ProductId,
                AdType = request.AdType,
                Title = request.Title,
                TextContent = request.AIGeneratedContent,
                ImageUrl = request.ImageUrl,
                VideoUrl = request.VideoUrl,
                Status = AISAM.Data.Enumeration.ContentStatusEnum.Draft
            };

            await _contentRepository.CreateAsync(content);

            PublishResultDto? publishResult = null;

            // If publish immediately, publish to specified integration
            if (request.PublishImmediately && request.IntegrationId.HasValue)
            {
                publishResult = await PublishContentAsync(content.Id, request.IntegrationId.Value);
                if (publishResult.Success)
                {
                    content.Status = AISAM.Data.Enumeration.ContentStatusEnum.Published;
                    await _contentRepository.UpdateAsync(content);

                    _logger.LogInformation("AI-generated content {ContentId} published successfully to integration {IntegrationId}",
                        content.Id, request.IntegrationId);
                }
                else
                {
                    content.Status = AISAM.Data.Enumeration.ContentStatusEnum.Rejected;
                    await _contentRepository.UpdateAsync(content);

                    _logger.LogError("Failed to publish AI-generated content {ContentId}: {Error}",
                        content.Id, publishResult.ErrorMessage);
                }
            }

            return MapToDto(content, publishResult);
        }

        private async Task<PublishResultDto> PublishContentAsync(Guid contentId, Guid integrationId)
        {
            // Get content
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = "Content not found"
                };
            }

            // Get social integration
            var integration = await _socialIntegrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = "Social integration not found"
                };
            }

            // Validate integration belongs to same user as content
            if (integration.UserId != content.Brand.UserId)
            {
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = "Integration does not belong to content owner"
                };
            }

            // Get provider service
            var platformName = integration.Platform.ToString().ToLower();
            if (!_providers.TryGetValue(platformName, out var provider))
            {
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = $"Provider '{platformName}' not supported"
                };
            }

            // Create post DTO for provider
            var postDto = new AISAM.Common.Models.PostDto
            {
                Message = content.TextContent,
                LinkUrl = null,
                ImageUrl = content.ImageUrl,
                VideoUrl = content.VideoUrl,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    title = content.Title ?? "",
                    ai_generated = true
                })
            };

            // Route by AdType
            if (content.AdType == AISAM.Data.Enumeration.AdTypeEnum.TextOnly)
            {
                // text only -> nothing else to set
            }
            else if (content.AdType == AISAM.Data.Enumeration.AdTypeEnum.VideoText)
            {
                postDto.VideoUrl = content.VideoUrl;
            }
            else if (content.AdType == AISAM.Data.Enumeration.AdTypeEnum.ImageText)
            {
                if (!string.IsNullOrWhiteSpace(content.ImageUrl))
                {
                    var raw = content.ImageUrl.Trim();
                    if (raw.StartsWith("["))
                    {
                        try
                        {
                            var urls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
                            postDto.ImageUrls = urls.Where(u => !string.IsNullOrWhiteSpace(u)).ToList();
                        }
                        catch
                        {
                            postDto.ImageUrl = content.ImageUrl;
                        }
                    }
                    else
                    {
                        postDto.ImageUrl = content.ImageUrl;
                    }
                }
            }

            try
            {
                var result = await provider.PublishAsync(integration.SocialAccount, integration, postDto);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully published AI-generated content {ContentId} to {Platform}",
                        contentId, platformName);
                }
                else
                {
                    _logger.LogError("Failed to publish AI-generated content {ContentId} to {Platform}: {Error}",
                        contentId, platformName, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while publishing AI-generated content {ContentId}", contentId);
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = $"Publishing failed: {ex.Message}"
                };
            }
        }

        private ContentResponseDto MapToDto(AISAM.Data.Model.Content content, PublishResultDto? publishResult)
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
    }
}