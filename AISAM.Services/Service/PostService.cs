using System.Text.Json;
using AISAM.Common.Dtos;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IContentRepository _contentRepository;
        private readonly ISocialIntegrationRepository _integrationRepository;
        private readonly ISocialAccountRepository _accountRepository;
        private readonly IContentCalendarRepository _calendarRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly IEnumerable<IProviderService> _providers;
        private readonly ILogger<PostService> _logger;

        public PostService(
            IPostRepository postRepository,
            IContentRepository contentRepository,
            ISocialIntegrationRepository integrationRepository,
            ISocialAccountRepository accountRepository,
            IContentCalendarRepository calendarRepository,
            INotificationRepository notificationRepository,
            IBrandRepository brandRepository,
            ITeamMemberRepository teamMemberRepository,
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            RolePermissionConfig rolePermissionConfig,
            IEnumerable<IProviderService> providers,
            ILogger<PostService> logger)
        {
            _postRepository = postRepository;
            _contentRepository = contentRepository;
            _integrationRepository = integrationRepository;
            _accountRepository = accountRepository;
            _calendarRepository = calendarRepository;
            _notificationRepository = notificationRepository;
            _brandRepository = brandRepository;
            _teamMemberRepository = teamMemberRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _providers = providers;
            _logger = logger;
        }

        public async Task<PagedResult<PostListItemDto>> GetPostsAsync(Guid requesterId, Guid? brandId, int page, int pageSize, ContentStatusEnum? status = null)
        {
            // Check if user has permission to view posts
            var canView = await CanUserPerformActionAsync(requesterId, "VIEW_POSTS", brandId);
            if (!canView)
            {
                throw new UnauthorizedAccessException("You are not allowed to view posts");
            }

            // If brandId is provided, we only need brandId to filter posts
            // The repository will handle the relationship through Content -> Brand
            Guid? profileId = null;
            
            // Only get profileId if no brandId is specified (for user's own posts)
            if (!brandId.HasValue)
            {
                var userProfiles = await _profileRepository.GetByUserIdAsync(requesterId);
                if (userProfiles == null || !userProfiles.Any())
                {
                    throw new UnauthorizedAccessException("User profile not found");
                }
                profileId = userProfiles.First().Id;
            }

            var posts = await _postRepository.GetPagedAsync(brandId, profileId, page, pageSize, includeDeleted: false, status);
            return new PagedResult<PostListItemDto>
            {
                Page = posts.Page,
                PageSize = posts.PageSize,
                TotalCount = posts.TotalCount,
                Data = posts.Data.Select(p => new PostListItemDto
                {
                    Id = p.Id,
                    ContentId = p.ContentId,
                    IntegrationId = p.IntegrationId,
                    ExternalPostId = p.ExternalPostId,
                    PublishedAt = p.PublishedAt,
                    Status = p.Status.ToString(),
                    IsDeleted = p.IsDeleted,
                    Link = GeneratePermalink(p.ExternalPostId)
                }).ToList()
            };
        }

        public async Task<PostListItemDto?> GetPostByIdAsync(Guid requesterId, Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                return null;
            }

            var brand = await _brandRepository.GetByIdAsync(post.Content.BrandId)
                ?? throw new ArgumentException("Thương hiệu không tồn tại");

            // Check if user has permission to view this post
            var canView = await CanUserPerformActionAsync(requesterId, "VIEW_POSTS", brand.Id);
            if (!canView)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem bài viết này");
            }

            return new PostListItemDto
            {
                Id = post.Id,
                ContentId = post.ContentId,
                IntegrationId = post.IntegrationId,
                ExternalPostId = post.ExternalPostId,
                PublishedAt = post.PublishedAt,
                Status = post.Status.ToString(),
                IsDeleted = post.IsDeleted,
                Link = GeneratePermalink(post.ExternalPostId)
            };
        }

        private static string? GeneratePermalink(string? externalPostId)
        {
            if (string.IsNullOrWhiteSpace(externalPostId))
            {
                return null;
            }
            // For Facebook, the composite id can be opened directly
            return $"https://www.facebook.com/{externalPostId}";
        }

        public async Task<bool> DeletePostAsync(Guid requesterId, Guid postId, bool isAdminOverride = false)
        {
            var post = await _postRepository.GetByIdAsync(postId)
                ?? throw new ArgumentException("Post không tồn tại");

            var brand = await _brandRepository.GetByIdAsync(post.Content.BrandId)
                ?? throw new ArgumentException("Thương hiệu không tồn tại");

            // Check if user has permission to delete this post
            var canDelete = isAdminOverride || await CanUserPerformActionAsync(requesterId, "DELETE_POST", brand.Id);
            if (!canDelete)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa bài viết này");
            }

            // If already published with external id, try platform delete; otherwise soft delete
            if (!string.IsNullOrWhiteSpace(post.ExternalPostId))
            {
                try
                {
                    var integration = await _integrationRepository.GetByIdAsync(post.IntegrationId)
                        ?? throw new ArgumentException("Integration không tồn tại");

                    var platformName = integration.Platform.ToString().ToLower();

                    var ok = await _postRepository.SoftDeleteAsync(postId);
                    if (ok)
                    {
                        await SendNotificationAsync(brand.ProfileId, "post_deleted", postId, "Post đã bị xóa");
                    }
                    return ok;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Xoá bài đăng trên nền tảng thất bại");
                    return false;
                }
            }
            else
            {
                var ok = await _postRepository.SoftDeleteAsync(postId);
                if (ok)
                {
                    await SendNotificationAsync(brand.ProfileId, "post_deleted", postId, "Post đã bị hủy");
                }
                return ok;
            }
        }

        private async Task SendNotificationAsync(Guid profileId, string type, Guid targetId, string? message)
        {
            if (!Enum.TryParse<NotificationTypeEnum>(type, true, out var notificationType))
            {
                notificationType = NotificationTypeEnum.SystemUpdate;
            }

            var noti = new Notification
            {
                ProfileId = profileId,
                Type = notificationType,
                TargetId = targetId,
                Message = message ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepository.CreateAsync(noti);
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

        public async Task<IEnumerable<PostListItemDto>> GetAllPostsAsync()
        {
            var posts = await _postRepository.GetAllAsync();
            return posts.Select(p => new PostListItemDto
            {
                Id = p.Id,
                ContentId = p.ContentId,
                IntegrationId = p.IntegrationId,
                ExternalPostId = p.ExternalPostId,
                PublishedAt = p.PublishedAt,
                Status = p.Status.ToString(),
                IsDeleted = p.IsDeleted,
                Link = GeneratePermalink(p.ExternalPostId)
            }).ToList();
        }
    }
}