using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
using Microsoft.Extensions.Logging;
using AISAM.Data.Model;

namespace AISAM.Services.Service
{
    public class ContentService : IContentService
    {
        private readonly IContentRepository _contentRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPostRepository _postRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<ContentService> _logger;
        private readonly Dictionary<string, IProviderService> _providers;

        public ContentService(
            IContentRepository contentRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IUserRepository userRepository,
            IPostRepository postRepository,
            IBrandRepository brandRepository,
            ITeamMemberRepository teamMemberRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<ContentService> logger,
            IEnumerable<IProviderService> providers)
        {
            _contentRepository = contentRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _userRepository = userRepository;
            _postRepository = postRepository;
            _brandRepository = brandRepository;
            _teamMemberRepository = teamMemberRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _logger = logger;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p);
        }

        public async Task<ContentResponseDto> CreateContentAsync(CreateContentRequest request, Guid userId)
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Check if user has permission to create content
            var canCreate = await CanUserPerformActionAsync(userId, "CREATE_CONTENT", request.BrandId);
            if (!canCreate)
            {
                throw new UnauthorizedAccessException("You are not allowed to create content");
            }

            // Create content entity
            var content = new Content
            {
                UserId = userId,
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
                publishResult = await PublishContentAsync(content.Id, request.IntegrationId.Value, userId);
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
                    
					// Create a rejected post record when immediate publish fails
					var failedPost = new Post
					{
						ContentId = content.Id,
						IntegrationId = request.IntegrationId.Value,
						ExternalPostId = null,
						PublishedAt = DateTime.UtcNow,
						Status = ContentStatusEnum.Rejected
					};
					await _postRepository.CreateAsync(failedPost);
					
                    _logger.LogError("Failed to publish content {ContentId}: {Error}", 
                        content.Id, publishResult.ErrorMessage);
                }
            }

            return MapToDto(content, publishResult);
        }

        public async Task<PublishResultDto> PublishContentAsync(Guid contentId, Guid integrationId, Guid userId)
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

            // Check if user has permission to publish content
            var canPublish = await CanUserPerformActionAsync(userId, "PUBLISH_POST", content.BrandId);
            if (!canPublish)
            {
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = "You are not allowed to publish this content"
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
                LinkUrl = null,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    title = content.Title ?? "",
                    style_description = content.StyleDescription ?? "",
                    context_description = content.ContextDescription ?? ""
                })
            };

            // Route by AdType
            if (content.AdType == AdTypeEnum.TextOnly)
            {
                // text only -> nothing else to set
            }
            else if (content.AdType == AdTypeEnum.VideoText)
            {
                postDto.VideoUrl = content.VideoUrl;
            }
            else if (content.AdType == AdTypeEnum.ImageText)
            {
                // image_url now jsonb. Accept string or JSON array stored as text
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
                            // fallback: treat as single url
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
                // Publish to provider using Model2 entities directly
                var result = await provider.PublishAsync(integration.SocialAccount, integration, postDto);
                
                if (result.Success)
                {
                    // Persist post record on successful publish
                    var post = new Post
                    {
                        ContentId = content.Id,
                        IntegrationId = integration.Id,
                        ExternalPostId = result.ProviderPostId,
                        PublishedAt = result.PostedAt ?? DateTime.UtcNow,
                        Status = ContentStatusEnum.Published
                    };
                    await _postRepository.CreateAsync(post);

                    // Update content status to published
                    content.Status = ContentStatusEnum.Published;
                    await _contentRepository.UpdateAsync(content);

                    // Removed audit logging per latest requirement

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

        public async Task<ContentResponseDto?> GetContentByIdAsync(Guid contentId, Guid userId)
        {
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                return null;
            }

            // Check if user has access to view this content
            var canView = await CanUserPerformActionAsync(userId, "CREATE_CONTENT", content.BrandId);
            if (!canView)
            {
                throw new UnauthorizedAccessException("You are not allowed to view this content");
            }

            return MapToDto(content, null);
        }

        

        public async Task<PagedResult<ContentResponseDto>> GetPagedContentsByBrandAsync(
            Guid brandId,
            PaginationRequest request,
            AdTypeEnum? adType = null,
            bool onlyDeleted = false,
            ContentStatusEnum? status = null)
        {
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            var (items, total) = await _contentRepository.GetByBrandIdPagedAsync(
                brandId,
                page,
                pageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                adType,
                onlyDeleted,
                status
            );

            return new PagedResult<ContentResponseDto>
            {
                Data = items.Select(c => MapToDto(c, null)).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> SoftDeleteAsync(Guid contentId)
        {
            var existing = await _contentRepository.GetByIdAsync(contentId);
            if (existing == null)
            {
                return false;
            }
            await _contentRepository.DeleteAsync(contentId);
            return true;
        }

        public async Task<bool> RestoreAsync(Guid contentId)
        {
            var existing = await _contentRepository.GetByIdIncludingDeletedAsync(contentId);
            if (existing == null || !existing.IsDeleted)
            {
                return false;
            }

            // Restore then reset status to Draft
            await _contentRepository.RestoreAsync(contentId);
            existing.Status = ContentStatusEnum.Draft;
            await _contentRepository.UpdateAsync(existing);
            return true;
        }

        /// <summary>
        /// Check if user can perform action with required permission
        /// </summary>
        private async Task<bool> CanUserPerformActionAsync(Guid userId, string permission, Guid? brandId = null)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // Admin users have all permissions
            if (user.Role == UserRoleEnum.Admin)
            {
                return true;
            }

            // If brandId is provided, check if user is brand owner or team member
            if (brandId.HasValue)
            {
                var brand = await _brandRepository.GetByIdAsync(brandId.Value);
                if (brand == null) return false;

                // User is brand owner
                if (brand.UserId == userId)
                {
                    return true;
                }

                // Check if user is team member of brand owner with required permission
                var teamMember = await _teamMemberRepository.GetByUserIdAsync(userId);
                if (teamMember == null) return false;

                // Check if team member belongs to the brand owner's vendor
                if (teamMember.Team.VendorId != brand.UserId) return false;

                // Check if team member has required permission
                return _rolePermissionConfig.HasCustomPermission(teamMember.Permissions, permission);
            }

            // Check team member's actual permissions (not role-based)
            var userTeamMember = await _teamMemberRepository.GetByUserIdAsync(userId);
            if (userTeamMember != null)
            {
                // Only check the actual permissions assigned to this team member
                return _rolePermissionConfig.HasCustomPermission(userTeamMember.Permissions, permission);
            }

            return false;
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