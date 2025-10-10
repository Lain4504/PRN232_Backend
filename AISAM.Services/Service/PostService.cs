using System.Text.Json;
using AISAM.Common.Dtos;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
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
            _providers = providers;
            _logger = logger;
        }

        public async Task<PagedResult<PostListItemDto>> GetPostsAsync(Guid requesterId, Guid? brandId, int page, int pageSize, ContentStatusEnum? status = null)
        {
            var posts = await _postRepository.GetPagedAsync(brandId, requesterId, page, pageSize, includeDeleted: false, status);
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
                    Status = p.Status,
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
            if (brand.UserId != requesterId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền");
            }

            return new PostListItemDto
            {
                Id = post.Id,
                ContentId = post.ContentId,
                IntegrationId = post.IntegrationId,
                ExternalPostId = post.ExternalPostId,
                PublishedAt = post.PublishedAt,
                Status = post.Status,
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
            if (!isAdminOverride && brand.UserId != requesterId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền");
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
                        await SendNotificationAsync(brand.UserId, "post_deleted", postId, "Post đã bị xóa");
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
                    await SendNotificationAsync(brand.UserId, "post_deleted", postId, "Post đã bị hủy");
                }
                return ok;
            }
        }

        private async Task SendNotificationAsync(Guid userId, string type, Guid targetId, string? message)
        {
            if (!Enum.TryParse<NotificationTypeEnum>(type, true, out var notificationType))
            {
                notificationType = NotificationTypeEnum.SystemUpdate;
            }

            var noti = new Notification
            {
                UserId = userId,
                Type = notificationType,
                TargetId = targetId,
                Message = message ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepository.CreateAsync(noti);
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
                Status = p.Status,
                IsDeleted = p.IsDeleted,
                Link = GeneratePermalink(p.ExternalPostId)
            }).ToList();
        }
    }
}