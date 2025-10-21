using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Common.Dtos.Request;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentEntity = AISAM.Data.Model.Content;
using AISAM.Repositories;
using DataModel = AISAM.Data.Model;

namespace AISAM.Services.Service
{
    public class AIService : IAIService
    {
        private readonly ILogger<AIService> _logger;
         private readonly GeminiSettings _settings;
         private readonly IUserRepository _userRepository;
        private readonly Dictionary<string, IProviderService> _providers;
        private readonly HttpClient _httpClient;
        private readonly IContentRepository _contentRepository;
        private readonly IAiGenerationRepository _aiGenerationRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly INotificationService _notificationService;
        private readonly IBrandRepository _brandRepository;
        private readonly IProductRepository _productRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly AisamContext _context;

        public AIService(
            IOptions<GeminiSettings> settings,
            ILogger<AIService> logger,
            HttpClient httpClient,
            IContentRepository contentRepository,
            IAiGenerationRepository aiGenerationRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            IBrandRepository brandRepository,
            IProductRepository productRepository,
            IConversationRepository conversationRepository,
            AisamContext context,
            IEnumerable<IProviderService> providers)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClient = httpClient;
            _contentRepository = contentRepository;
            _aiGenerationRepository = aiGenerationRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _brandRepository = brandRepository;
            _productRepository = productRepository;
            _conversationRepository = conversationRepository;
            _context = context;
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

            return await CreateAndProcessAiGenerationAsync(content.Id, request.AIGenerationPrompt);
        }

        public async Task<AiGenerationResponse> ImproveContentAsync(Guid contentId, string improvementPrompt)
        {
            // Get existing content
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            return await CreateAndProcessAiGenerationAsync(contentId, improvementPrompt);
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

        private async Task<AiGenerationResponse> CreateAndProcessAiGenerationAsync(Guid contentId, string prompt)
        {
            var aiGeneration = new AiGeneration
            {
                ContentId = contentId,
                AiPrompt = prompt,
                Status = AiStatusEnum.Pending
            };

            await _aiGenerationRepository.CreateAsync(aiGeneration);

            try
            {
                var generatedText = await GenerateContentWithGemini(prompt);

                aiGeneration.GeneratedText = generatedText;
                aiGeneration.Status = AiStatusEnum.Completed;

                await _aiGenerationRepository.UpdateAsync(aiGeneration);

                return new AiGenerationResponse
                {
                    AiGenerationId = aiGeneration.Id,
                    ContentId = contentId,
                    GeneratedText = generatedText,
                    Status = AiStatusEnum.Completed,
                    CreatedAt = aiGeneration.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI content for content {ContentId}", contentId);

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
                // Use the model from settings, fallback to working model
                var model = _settings.Model ?? "gemini-2.5-flash";
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_settings.ApiKey}";

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
                        maxOutputTokens = _settings.MaxTokens,
                        temperature = _settings.Temperature
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>();
                    if (result?.Candidates?.Length > 0 && result.Candidates[0].Content?.Parts?.Length > 0)
                    {
                        return result.Candidates[0].Content?.Parts?[0].Text?.Trim() ?? "No content generated";
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                _logger.LogWarning("Request URL: {Url}", url);
                _logger.LogWarning("Request Body: {Body}", JsonSerializer.Serialize(requestBody));
                throw new Exception($"Gemini API error: {response.StatusCode} - {errorContent}");
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
            [JsonPropertyName("candidates")]
            public Candidate[]? Candidates { get; set; }
            [JsonPropertyName("usageMetadata")]
            public UsageMetadata? UsageMetadata { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public Part[]? Parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private class UsageMetadata
        {
            [JsonPropertyName("totalTokenCount")]
            public int TotalTokenCount { get; set; }
        }

        public async Task<ChatResponse> ChatWithAIAsync(ChatRequest request)
        {
            try
            {
                // Validate user exists
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }

                // Get or create conversation
                var conversation = await GetOrCreateConversationAsync(request);

                // Save user message
                await SaveChatMessageAsync(conversation.Id, 0, request.Message);

            // Get brand context (optional)
            AISAM.Data.Model.Brand? brand = null;
            if (request.BrandId.HasValue && request.BrandId.Value != Guid.Empty)
            {
                try
                {
                    brand = await _brandRepository.GetByIdAsync(request.BrandId.Value);
                    if (brand == null || brand.IsDeleted)
                    {
                        _logger.LogWarning("Brand not found or deleted for ID {BrandId}, continuing without brand context", request.BrandId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error fetching brand for ID {BrandId}, continuing without brand context", request.BrandId.Value);
                }
            }

            // Get product context if provided
            AISAM.Data.Model.Product? product = null;
            if (request.ProductId.HasValue)
            {
                product = await _productRepository.GetByIdAsync(request.ProductId.Value);
                if (product == null)
                {
                    throw new ArgumentException("Product not found");
                }
            }

            // Build enhanced prompt with brand and product context
            var enhancedPrompt = BuildEnhancedPrompt(request.Message, brand, product, request.AdType);

            // Check if user wants to generate content
            var shouldGenerateContent = ShouldGenerateContent(request.Message.ToLower());

            if (shouldGenerateContent)
            {
                // Validate that we have a brand for content creation
                if (brand == null)
                {
                    // Save AI response message
                    await SaveChatMessageAsync(conversation.Id, 1, "I'd love to create content for you, but I need to know which brand this content is for. Please select a brand first.");

                    return new ChatResponse
                    {
                        Response = "I'd love to create content for you, but I need to know which brand this content is for. Please select a brand first.",
                        IsContentGenerated = false,
                        ConversationId = conversation.Id
                    };
                }

                // Create draft content and generate AI content
                var content = new ContentEntity
                {
                    BrandId = brand.Id, // Brand is guaranteed to be non-null here
                    ProductId = request.ProductId,
                    AdType = request.AdType,
                    Title = $"AI Generated Content - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    TextContent = "", // Will be filled by AI
                    Status = ContentStatusEnum.Draft
                };

                await _contentRepository.CreateAsync(content);

                var aiGeneration = await CreateAndProcessAiGenerationAsync(content.Id, enhancedPrompt);

                // If AI generation succeeded, update content with generated text
                if (aiGeneration.Status == AiStatusEnum.Completed && !string.IsNullOrEmpty(aiGeneration.GeneratedText))
                {
                    content.TextContent = aiGeneration.GeneratedText;
                    await _contentRepository.UpdateAsync(content);
                }

                var responseMessage = "I've created content for you based on your brand and product. Here's what I generated:";

                // Save AI response message
                await SaveChatMessageAsync(conversation.Id, 1, responseMessage, aiGeneration.AiGenerationId, content.Id);

                return new ChatResponse
                {
                    Response = responseMessage,
                    IsContentGenerated = true,
                    ContentId = content.Id,
                    AiGenerationId = aiGeneration.AiGenerationId,
                    GeneratedContent = aiGeneration.GeneratedText ?? "Content generation failed, but you can try again or approve a different version.",
                    ConversationId = conversation.Id
                };
            }
            else
            {
                // Just chat - generate response without creating content
                var chatPrompt = $"You are a helpful AI assistant for social media content creation. {(brand != null ? $"The user has selected brand '{brand.Name}'" : "No brand selected")}{(product != null ? $" and product '{product.Name}'" : "")}. They want to create {request.AdType.ToString().ToLower()} content. Respond helpfully to their message: '{request.Message}'";

                var aiResponse = await GenerateContentWithGemini(chatPrompt);

                // Save AI response message
                await SaveChatMessageAsync(conversation.Id, 1, aiResponse);

                return new ChatResponse
                {
                    Response = aiResponse,
                    IsContentGenerated = false,
                    ConversationId = conversation.Id
                };
            }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatWithAIAsync: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                throw new Exception("Failed to process AI chat request", ex);
            }
        }

        private async Task<Data.Model.Conversation> GetOrCreateConversationAsync(ChatRequest request)
        {
            // Always create a new conversation for each chat request
            // This ensures each chat session gets its own conversation
            var conversation = new Data.Model.Conversation
            {
                ProfileId = request.UserId,
                BrandId = request.BrandId,
                ProductId = request.ProductId,
                AdType = request.AdType,
                Title = $"AI Chat - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _conversationRepository.CreateAsync(conversation);
        }

        private async Task SaveChatMessageAsync(Guid conversationId, int senderType, string message,
            Guid? aiGenerationId = null, Guid? contentId = null)
        {
            var chatMessage = new AISAM.Data.Model.ChatMessage
            {
                ConversationId = conversationId,
                SenderType = (AISAM.Data.Model.ChatSenderType)senderType,
                Message = message,
                AiGenerationId = aiGenerationId,
                ContentId = contentId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ChatMessages.AddAsync(chatMessage);
            await _context.SaveChangesAsync();
        }

        private string BuildEnhancedPrompt(string userMessage, AISAM.Data.Model.Brand? brand, AISAM.Data.Model.Product? product, AdTypeEnum adType)
        {
            var prompt = brand != null
                ? $"Create {adType.ToString().ToLower()} content for brand '{brand.Name}'"
                : $"Create {adType.ToString().ToLower()} content";

            if (brand != null)
            {
                if (!string.IsNullOrEmpty(brand.Description))
                {
                    prompt += $"\nBrand Description: {brand.Description}";
                }

                if (!string.IsNullOrEmpty(brand.Slogan))
                {
                    prompt += $"\nBrand Slogan: {brand.Slogan}";
                }

                if (!string.IsNullOrEmpty(brand.Usp))
                {
                    prompt += $"\nUnique Selling Points: {brand.Usp}";
                }

                if (!string.IsNullOrEmpty(brand.TargetAudience))
                {
                    prompt += $"\nTarget Audience: {brand.TargetAudience}";
                }
            }

            if (product != null)
            {
                prompt += $"\n\nProduct: {product.Name}";
                if (!string.IsNullOrEmpty(product.Description))
                {
                    prompt += $"\nProduct Description: {product.Description}";
                }
                if (product.Price.HasValue)
                {
                    prompt += $"\nProduct Price: ${product.Price.Value}";
                }
            }

            prompt += $"\n\nUser Request: {userMessage}";
            prompt += $"\n\nGenerate engaging, professional content that aligns with the brand's voice and appeals to the target audience.";

            return prompt;
        }

        private bool ShouldGenerateContent(string message)
        {
            var generateKeywords = new[]
            {
                "create", "generate", "make", "write", "design", "produce",
                "help me create", "give me", "i want", "i need",
                "content for", "post about", "advertisement for"
            };

            return generateKeywords.Any(keyword => message.Contains(keyword));
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