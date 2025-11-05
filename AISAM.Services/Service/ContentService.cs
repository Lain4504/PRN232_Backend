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
        private readonly IApprovalRepository _approvalRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPostRepository _postRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<ContentService> _logger;
        private readonly Dictionary<string, IProviderService> _providers;

        public ContentService(
            IContentRepository contentRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IApprovalRepository approvalRepository,
            IUserRepository userRepository,
            IPostRepository postRepository,
            IBrandRepository brandRepository,
            ITeamMemberRepository teamMemberRepository,
            IProfileRepository profileRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<ContentService> logger,
            IEnumerable<IProviderService> providers)
        {
            _contentRepository = contentRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _approvalRepository = approvalRepository;
            _userRepository = userRepository;
            _postRepository = postRepository;
            _brandRepository = brandRepository;
            _teamMemberRepository = teamMemberRepository;
            _profileRepository = profileRepository;
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

            // Get brand to get the profile ID (content belongs to brand owner's profile)
            var brand = await _brandRepository.GetByIdAsync(request.BrandId);
            if (brand == null)
            {
                throw new ArgumentException("Brand not found");
            }

            // Check subscription and quota for the profile (brand owner)
            var canCreateQuota = await CheckSubscriptionAndQuotaForProfile(brand.ProfileId, "CREATE_CONTENT");
            if (!canCreateQuota)
            {
                throw new UnauthorizedAccessException("Subscription or quota limit exceeded");
            }

            // Create content entity
            var content = new Content
            {
                ProfileId = brand.ProfileId,  // Content belongs to brand owner's profile
                BrandId = request.BrandId,
                ProductId = request.ProductId,
                AdType = request.AdType,
                Title = request.Title,
                TextContent = request.TextContent,
                ImageUrl = FormatImageUrlForJsonb(request.ImageUrl),
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

            return MapToDto(content, publishResult, brand.Name);
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

            // Prevent republish if content already published
            if (content.Status == ContentStatusEnum.Published)
            {
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = "Content has already been published"
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
            if (integration.ProfileId != content.Brand.ProfileId) // Assuming Brand has ProfileId
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
                            var validUrls = urls.Where(u => !string.IsNullOrWhiteSpace(u)).ToList();
                            
                            if (validUrls.Count == 1)
                            {
                                // Single image - use ImageUrl for /photos endpoint
                                postDto.ImageUrl = validUrls[0];
                            }
                            else if (validUrls.Count > 1)
                            {
                                // Multiple images - use ImageUrls for /feed with attached_media
                                postDto.ImageUrls = validUrls;
                            }
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

                    // Update related approvals to Published to lock further publishes
                    var approvals = await _approvalRepository.GetByContentIdAsync(content.Id);
                    foreach (var approval in approvals)
                    {
                        if (approval.Status == ContentStatusEnum.Approved)
                        {
                            approval.Status = ContentStatusEnum.Published;
                            await _approvalRepository.UpdateAsync(approval);
                        }
                    }

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

        public async Task<ContentResponseDto> CloneContentAsync(Guid contentId, Guid userId)
        {
            // Get existing content
            var existing = await _contentRepository.GetByIdAsync(contentId);
            if (existing == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Permission: user must be able to create content for this brand
            var canCreate = await CanUserPerformActionAsync(userId, "CREATE_CONTENT", existing.BrandId);
            if (!canCreate)
            {
                throw new UnauthorizedAccessException("You are not allowed to clone this content");
            }

            var clone = new Content
            {
                ProfileId = existing.ProfileId,
                BrandId = existing.BrandId,
                ProductId = existing.ProductId,
                AdType = existing.AdType,
                Title = existing.Title,
                TextContent = existing.TextContent,
                ImageUrl = existing.ImageUrl,
                VideoUrl = existing.VideoUrl,
                StyleDescription = existing.StyleDescription,
                ContextDescription = existing.ContextDescription,
                RepresentativeCharacter = existing.RepresentativeCharacter,
                Status = ContentStatusEnum.Draft
            };

            await _contentRepository.CreateAsync(clone);

            var brand = await _brandRepository.GetByIdAsync(clone.BrandId);
            return MapToDto(clone, null, brand?.Name);
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

            var brand = await _brandRepository.GetByIdAsync(content.BrandId);
            return MapToDto(content, null, brand?.Name);
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

            // Prefetch brand name once for this brand scope
            var brand = await _brandRepository.GetByIdAsync(brandId);
            return new PagedResult<ContentResponseDto>
            {
                Data = items.Select(c => MapToDto(c, null, brand?.Name)).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<ContentResponseDto>> GetPagedContentsAsync(
            Guid? brandId,
            Guid userId,
            PaginationRequest request,
            AdTypeEnum? adType = null,
            bool onlyDeleted = false,
            ContentStatusEnum? status = null)
        {
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            var (items, total) = await _contentRepository.GetPagedAsync(
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

            // Map items to DTOs with brand names
            var brandIds = items.Select(c => c.BrandId).Distinct().ToList();
            // Fetch all brands in a single query to avoid concurrent DbContext operations
            var brands = await _brandRepository.GetByIdsAsync(brandIds);
            var brandMap = brands.ToDictionary(b => b.Id, b => b.Name);

            var data = items.Select(c => MapToDto(c, null, brandMap.TryGetValue(c.BrandId, out var brandName) ? brandName : null)).ToList();

            return new PagedResult<ContentResponseDto>
            {
                Data = data,
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

        public async Task<ContentResponseDto> UpdateContentAsync(Guid contentId, UpdateContentRequest request, Guid userId)
        {
            // Get existing content
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Check if user has permission to update this content
            var canUpdate = await CanUserPerformActionAsync(userId, "EDIT_CONTENT", content.BrandId);
            if (!canUpdate)
            {
                throw new UnauthorizedAccessException("You are not allowed to update this content");
            }

            // Check if content can be updated (only draft content can be updated)
            if (content.Status != ContentStatusEnum.Draft)
            {
                throw new ArgumentException("Only draft content can be updated");
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.Title))
            {
                content.Title = request.Title;
            }

            if (!string.IsNullOrEmpty(request.TextContent))
            {
                content.TextContent = request.TextContent;
            }

            if (request.AdType.HasValue)
            {
                content.AdType = request.AdType.Value;
            }

            if (request.ProductId.HasValue)
            {
                content.ProductId = request.ProductId.Value;
            }

            if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                content.ImageUrl = FormatImageUrlForJsonb(request.ImageUrl);
            }

            if (!string.IsNullOrEmpty(request.VideoUrl))
            {
                content.VideoUrl = request.VideoUrl;
            }

            if (!string.IsNullOrEmpty(request.StyleDescription))
            {
                content.StyleDescription = request.StyleDescription;
            }

            if (!string.IsNullOrEmpty(request.ContextDescription))
            {
                content.ContextDescription = request.ContextDescription;
            }

            if (!string.IsNullOrEmpty(request.RepresentativeCharacter))
            {
                content.RepresentativeCharacter = request.RepresentativeCharacter;
            }

            // Update the content
            await _contentRepository.UpdateAsync(content);

            _logger.LogInformation("Updated content {ContentId} by user {UserId}", contentId, userId);

            var brand = await _brandRepository.GetByIdAsync(content.BrandId);
            return MapToDto(content, null, brand?.Name);
        }

        /// <summary>
        /// Check if user can perform action with required permission
        /// </summary>
        private async Task<bool> CanUserPerformActionAsync(Guid userId, string permission, Guid? brandId = null)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // If brandId is provided, check if user is brand owner or team member
            if (brandId.HasValue)
            {
                var brand = await _brandRepository.GetByIdAsync(brandId.Value);
                if (brand == null) return false;

                // Check direct ownership through user's profiles
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
                if (teamMember == null) return false;

                return _rolePermissionConfig.HasCustomPermission(teamMember.Permissions, permission);
            }

            // Fallback: check if user has permission in any team membership
            var userTeamMembers = await _teamMemberRepository.GetByUserIdWithBrandsAsync(userId);
            if (userTeamMembers != null && userTeamMembers.Any())
            {
                return userTeamMembers.Any(tm => _rolePermissionConfig.HasCustomPermission(tm.Permissions, permission));
            }

            return false;
        }

        /// <summary>
        /// Check subscription and quota for profile (used for content creation)
        /// </summary>
        private async Task<bool> CheckSubscriptionAndQuotaForProfile(Guid profileId, string operation)
        {
            // Get profile and subscription
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null) return false;

            // For Free profiles, no subscription check needed
            if (profile.ProfileType == ProfileTypeEnum.Free)
            {
                return true;
            }

            // For Basic/Pro profiles, check subscription
            // TODO: Implement subscription repository and check active subscription
            // var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
            // if (subscription?.IsActive != true) return false;

            // TODO: Implement quota checking
            // var quotaCheck = await _quotaService.CheckQuotaForProfile(profileId, operation);
            // return quotaCheck;

            return true; // Temporary: allow all for now
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

        private ContentResponseDto MapToDto(Content content, PublishResultDto? publishResult, string? brandName = null)
        {
            return new ContentResponseDto
            {
                Id = content.Id,
                ProfileId = content.ProfileId,
                BrandId = content.BrandId,
                BrandName = brandName,
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