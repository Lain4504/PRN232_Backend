using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using AISAM.Data.Model;

namespace AISAM.Services.Service
{
    public class ContentService : IContentService
    {
        private readonly IContentRepository _contentRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ContentService> _logger;
        private readonly Dictionary<string, IProviderService> _providers;

        public ContentService(
            IContentRepository contentRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IUserRepository userRepository,
            ILogger<ContentService> logger,
            IEnumerable<IProviderService> providers)
        {
            _contentRepository = contentRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _userRepository = userRepository;
            _logger = logger;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p);
        }

        public async Task<ContentResponseDto> CreateContentAsync(CreateContentRequest request)
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Create content entity
            var content = new Content
            {
                BrandId = request.BrandId,
                ProductId = request.ProductId,
                AdType = request.AdType,
                Title = request.Title,
                TextContent = request.TextContent,
                ImageUrl = request.ImageUrl,
                VideoUrl = request.VideoUrl,
                StyleDescription = request.StyleDescription,
                ContextDescription = request.ContextDescription,
                RepresentativeCharacter = request.RepresentativeCharacter,
                Status = ContentStatusEnum.Draft
            };

            await _contentRepository.CreateAsync(content);

            PublishResultDto? publishResult = null;

            // If publish immediately, publish to specified integration
            if (request.PublishImmediately && request.IntegrationId.HasValue)
            {
                publishResult = await PublishContentAsync(content.Id, request.IntegrationId.Value);
                if (publishResult.Success)
                {
                    content.Status = ContentStatusEnum.Published;
                    await _contentRepository.UpdateAsync(content);
                    
                    _logger.LogInformation("Content {ContentId} published successfully to integration {IntegrationId}", 
                        content.Id, request.IntegrationId);
                }
                else
                {
                    content.Status = ContentStatusEnum.Rejected;
                    await _contentRepository.UpdateAsync(content);
                    
                    _logger.LogError("Failed to publish content {ContentId}: {Error}", 
                        content.Id, publishResult.ErrorMessage);
                }
            }

            return MapToDto(content, publishResult);
        }

        public async Task<PublishResultDto> PublishContentAsync(Guid contentId, Guid integrationId)
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
            if (integration.UserId != content.Brand.UserId) // Assuming Brand has UserId
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
            var postDto = new PostDto
            {
                Message = content.TextContent,
                LinkUrl = null, // Could be added to Content entity if needed
                ImageUrl = content.ImageUrl,
                VideoUrl = content.VideoUrl,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    title = content.Title ?? "",
                    style_description = content.StyleDescription ?? "",
                    context_description = content.ContextDescription ?? ""
                })
            };

            try
            {
                // Publish to provider using Model2 entities directly
                var result = await provider.PublishAsync(integration.SocialAccount, integration, postDto);
                
                if (result.Success)
                {
                    _logger.LogInformation("Successfully published content {ContentId} to {Platform}", 
                        contentId, platformName);
                }
                else
                {
                    _logger.LogError("Failed to publish content {ContentId} to {Platform}: {Error}", 
                        contentId, platformName, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while publishing content {ContentId}", contentId);
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = $"Publishing failed: {ex.Message}"
                };
            }
        }

        public async Task<ContentResponseDto?> GetContentByIdAsync(Guid contentId)
        {
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                return null;
            }

            return MapToDto(content, null);
        }

        public async Task<IEnumerable<ContentResponseDto>> GetUserContentsAsync(Guid userId)
        {
            var contents = await _contentRepository.GetByUserIdAsync(userId);
            return contents.Select(c => MapToDto(c, null));
        }

        private ContentResponseDto MapToDto(Content content, PublishResultDto? publishResult)
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
                StyleDescription = content.StyleDescription,
                ContextDescription = content.ContextDescription,
                RepresentativeCharacter = content.RepresentativeCharacter,
                Status = content.Status.ToString(),
                CreatedAt = content.CreatedAt,
                UpdatedAt = content.UpdatedAt,
                ExternalPostId = publishResult?.ProviderPostId,
                PublishedAt = publishResult?.PostedAt
            };
        }
    }

}
