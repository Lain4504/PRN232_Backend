using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Common.Dtos.Request;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentEntity = AISAM.Data.Model.Content;
using AISAM.Repositories;
using DataModel = AISAM.Data.Model;
using Microsoft.EntityFrameworkCore;

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
        private readonly IProfileRepository _profileRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
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
            IProfileRepository profileRepository,
            ITeamMemberRepository teamMemberRepository,
            RolePermissionConfig rolePermissionConfig,
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
            _profileRepository = profileRepository;
            _teamMemberRepository = teamMemberRepository;
            _rolePermissionConfig = rolePermissionConfig;
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

            // Get brand to get the profile ID (content belongs to brand owner's profile)
            var brand = await _brandRepository.GetByIdAsync(request.BrandId);
            if (brand == null)
            {
                throw new ArgumentException("Brand not found");
            }

            // Create draft content
            var content = new ContentEntity
            {
                ProfileId = brand.ProfileId,  // Content belongs to brand owner's profile
                BrandId = request.BrandId,
                ProductId = request.ProductId,
                AdType = request.AdType,
                Title = request.Title,
                TextContent = "", // Will be filled by AI
                ImageUrl = FormatImageUrlForJsonb(request.ImageUrl),
                VideoUrl = request.VideoUrl,
                Status = ContentStatusEnum.Draft
            };

            await _contentRepository.CreateAsync(content);

            return await CreateAndProcessAiGenerationAsync(content.Id, request.AIGenerationPrompt, request.AdType);
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

            // Update content with AI-generated text and image
            content.TextContent = aiGeneration.GeneratedText;
            if (!string.IsNullOrEmpty(aiGeneration.GeneratedImageUrl))
            {
                content.ImageUrl = FormatImageUrlForJsonb(aiGeneration.GeneratedImageUrl);
            }
            content.Status = ContentStatusEnum.Approved; // Mark as approved
            content.UpdatedAt = DateTime.UtcNow;

            await _contentRepository.UpdateAsync(content);

            _logger.LogInformation("AI generation {AiGenerationId} approved and copied to content {ContentId}",
                aiGenerationId, content.Id);

            return MapToContentDto(content, null);
        }

        private async Task<AiGenerationResponse> CreateAndProcessAiGenerationAsync(Guid contentId, string prompt, AdTypeEnum adType = AdTypeEnum.TextOnly)
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

                if (adType == AdTypeEnum.ImageText)
                {
                    // Generate an optimized image prompt using Gemini based on the context
                    var imagePromptRequest = $"Create a detailed, high-quality visual description for an AI image generator (like DALL-E or Midjourney). " +
                        $"The image is for an advertisement based on this context: {prompt}. " +
                        $"Focus on composition, lighting, style, and professional aesthetic. " +
                        $"Return ONLY the image prompt text, no explanations.";
                    
                    var visualPrompt = await GenerateContentWithGemini(imagePromptRequest);
                    
                    // Use Pollinations.ai for the visual generation
                    var seed = new Random().Next(1000000);
                    var encodedPrompt = Uri.EscapeDataString(visualPrompt);
                    var imageUrl = $"https://image.pollinations.ai/prompt/{encodedPrompt}?width=1024&height=1024&nologo=true&seed={seed}&model=flux";
                    
                    aiGeneration.GeneratedImageUrl = imageUrl;
                    aiGeneration.GeneratedText = generatedText;
                }
                else
                {
                    aiGeneration.GeneratedText = generatedText;
                }

                aiGeneration.Status = AiStatusEnum.Completed;

                await _aiGenerationRepository.UpdateAsync(aiGeneration);

                return new AiGenerationResponse
                {
                    AiGenerationId = aiGeneration.Id,
                    ContentId = contentId,
                    GeneratedText = generatedText,
                    GeneratedImageUrl = aiGeneration.GeneratedImageUrl,
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

                // Validate user has permission to access AI chat for this brand
                var canAccess = await CanUserAccessAIChatAsync(request.UserId, request.BrandId);
                if (!canAccess)
                {
                    throw new UnauthorizedAccessException("You are not allowed to use AI chat for this brand");
                }

                // Get the effective profile ID for the conversation
                // This could be either the user's own profile or the brand owner's profile (for team members)
                var effectiveProfileId = await GetEffectiveProfileIdAsync(request.ProfileId, request.BrandId);
                if (effectiveProfileId == Guid.Empty)
                {
                    throw new ArgumentException("No valid profile found for this request");
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
                    ProfileId = brand.ProfileId,  // Content belongs to brand owner's profile
                    BrandId = brand.Id, // Brand is guaranteed to be non-null here
                    ProductId = request.ProductId,
                    AdType = request.AdType,
                    Title = $"AI Generated Content - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    TextContent = "", // Will be filled by AI
                    Status = ContentStatusEnum.Draft
                };

                await _contentRepository.CreateAsync(content);

                var aiGeneration = await CreateAndProcessAiGenerationAsync(content.Id, enhancedPrompt, request.AdType);

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
                    GeneratedImageUrl = aiGeneration.GeneratedImageUrl,
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
            // Get the effective profile ID for the conversation
            var effectiveProfileId = await GetEffectiveProfileIdAsync(request.ProfileId, request.BrandId);
            
            // Check if we have an existing conversation ID and it's valid
            if (request.ConversationId.HasValue)
            {
                var existingConversation = await _conversationRepository.GetByIdAsync(request.ConversationId.Value);
                if (existingConversation != null && existingConversation.IsActive && !existingConversation.IsDeleted)
                {
                    // Update the conversation's updated timestamp
                    existingConversation.UpdatedAt = DateTime.UtcNow;
                    await _conversationRepository.UpdateAsync(existingConversation);
                    return existingConversation;
                }
            }
            
            // Create a new conversation if no valid existing conversation is found
            var conversation = new Data.Model.Conversation
            {
                ProfileId = effectiveProfileId,
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
            
            if (adType == AdTypeEnum.ImageText)
            {
                prompt += "\n\nThis is an image-based advertisement. Please generate a compelling caption/text and also keep the visual context in mind for the image prompt.";
            }

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

        /// <summary>
        /// Get the effective profile ID for the conversation
        /// Always use the brand owner's profile for quota and billing purposes
        /// </summary>
        private async Task<Guid> GetEffectiveProfileIdAsync(Guid requestedProfileId, Guid? brandId)
        {
            // If brandId is provided, always use the brand owner's profile
            // This ensures quota and billing are charged to the correct profile
            if (brandId.HasValue)
            {
                var brand = await _brandRepository.GetByIdAsync(brandId.Value);
                if (brand != null)
                {
                    return brand.ProfileId; // Always use brand owner's profile
                }
            }

            // Fallback: if no brand specified, use the requested profile
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Id == requestedProfileId && !p.IsDeleted);
            if (profile != null)
            {
                return profile.Id;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Validate if user can access AI chat for the given brand
        /// </summary>
        private async Task<bool> CanUserAccessAIChatAsync(Guid userId, Guid? brandId)
        {
            if (!brandId.HasValue)
            {
                return true; // No brand restriction
            }

            var brand = await _brandRepository.GetByIdAsync(brandId.Value);
            if (brand == null)
            {
                return false;
            }

            // Check if user is brand owner (through any of their profiles)
            var userProfiles = await _profileRepository.GetByUserIdAsync(userId);
            if (userProfiles?.Any(p => p.Id == brand.ProfileId) == true)
            {
                return true; // User owns this brand directly
            }

            // If brand's profile is Free type, only owner can access
            var brandProfile = await _profileRepository.GetByIdAsync(brand.ProfileId);
            if (brandProfile?.ProfileType == ProfileTypeEnum.Free)
            {
                return false; // Free profiles don't have team features
            }

            // For Basic/Pro profiles: check team member access
            var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brandId.Value);
            if (teamMember == null)
            {
                return false;
            }

            // Check if team member has permission to use AI chat
            return _rolePermissionConfig.HasCustomPermission(teamMember.Permissions, "CREATE_CONTENT");
        }

        private ContentResponseDto MapToContentDto(ContentEntity content, PublishResultDto? publishResult)
        {
            return new ContentResponseDto
            {
                Id = content.Id,
                ProfileId = content.ProfileId,
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

        /// <summary>
        /// Format ImageUrl for jsonb column - convert single URL to JSON string or keep JSON array as is
        /// </summary>
        private string? FormatImageUrlForJsonb(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            var trimmed = imageUrl.Trim();
            
            // If it's already a JSON array, return as is
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                return trimmed;
            }
            
            // If it's a single URL, wrap it in a JSON array
            return System.Text.Json.JsonSerializer.Serialize(new[] { trimmed });
        }
    }
}