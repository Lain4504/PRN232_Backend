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
        private readonly IPerformanceReportRepository _reportRepository;
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
            IPerformanceReportRepository reportRepository,
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
            _reportRepository = reportRepository;
            _calendarRepository = calendarRepository;
            _notificationRepository = notificationRepository;
            _brandRepository = brandRepository;
            _providers = providers;
            _logger = logger;
        }

        public async Task<PublishResultDto> PublishPostAsync(Guid userId, Guid contentId, IEnumerable<Guid> integrationIds)
        {
            var content = await _contentRepository.GetByIdAsync(contentId);
            var brand = await _brandRepository.GetByIdAsync(content!.BrandId);

            var results = new List<PublishResultDto>();

            foreach (var integrationId in integrationIds)
            {
                var integration = await _integrationRepository.GetByIdAsync(integrationId);
                var provider = ResolveProvider(integration!.Platform);

                var postDto = new PostDto
                {
                    Message = content.TextContent ?? string.Empty,
                    ImageUrls = !string.IsNullOrEmpty(content.ImageUrl) ? new List<string> { content.ImageUrl } : new List<string>(),
                    VideoUrl = content.VideoUrl,
                    LinkUrl = null
                };

                var result = await provider.PublishAsync(integration.SocialAccount, integration, postDto);
                if (result.Success)
                {
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

                    await SendNotificationAsync(brand.UserId, "post_published", post.Id, $"Đã xuất bản lên {integration.Platform}");
                }
                else
                {
                    await SendNotificationAsync(brand.UserId, "post_failed", content.Id, result.ErrorMessage ?? "Xuất bản thất bại");
                }

                results.Add(result);
            }

            // Aggregate result (success if at least one success)
            var anySuccess = results.Any(r => r.Success);
            return new PublishResultDto
            {
                Success = anySuccess,
                ErrorMessage = anySuccess ? null : string.Join("; ", results.Select(r => r.ErrorMessage).Where(e => !string.IsNullOrWhiteSpace(e)))
            };
        }

        public async Task<Guid> SchedulePostAsync(Guid userId, Guid contentId, Guid integrationId, DateTime scheduledAtUtc)
        {

            var schedule = new ContentCalendar
            {
                ContentId = contentId,
                ScheduledDate = scheduledAtUtc.Date,
                ScheduledTime = scheduledAtUtc.TimeOfDay,
                IsDeleted = false
            };
            schedule = await _calendarRepository.CreateAsync(schedule);

            var content = await _contentRepository.GetByIdAsync(contentId);
            var brand = await _brandRepository.GetByIdAsync(content!.BrandId);

            await SendNotificationAsync(brand!.UserId, "post_scheduled", schedule.Id, "Post scheduled");

            return schedule.Id;
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
                    IsDeleted = p.IsDeleted
                }).ToList()
            };
        }

        public async Task<bool> DeleteOrCancelPostAsync(Guid requesterId, Guid postId, bool isAdminOverride = false)
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

        public async Task<bool> PullReportsAsync(Guid requesterId, Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId)
                ?? throw new ArgumentException("Post không tồn tại");
            var brand = await _brandRepository.GetByIdAsync(post.Content.BrandId)
                ?? throw new ArgumentException("Thương hiệu không tồn tại");
            if (brand.UserId != requesterId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền");
            }

            var integration = await _integrationRepository.GetByIdAsync(post.IntegrationId)
                ?? throw new ArgumentException("Integration không tồn tại");
            var provider = ResolveProvider(integration.Platform);

            // Fake insights for demo; integrate provider insights API here
            var raw = JsonSerializer.Serialize(new { impressions = 100, engagement = 5, ctr = 0.05m });

            var report = new PerformanceReport
            {
                PostId = post.Id,
                ReportDate = DateTime.UtcNow.Date,
                Impressions = 100,
                Engagement = 5,
                Ctr = 0.05m,
                EstimatedRevenue = 0,
                RawData = raw
            };
            await _reportRepository.UpsertAsync(report);

            // Optional notification for low CTR
            if (report.Ctr < 0.01m)
            {
                await SendNotificationAsync(brand.UserId, "performance_alert", post.Id, "Low CTR detected");
            }

            return true;
        }

        public async Task<bool> ReAuthIntegrationAsync(Guid requesterId, Guid integrationId)
        {
            var integration = await _integrationRepository.GetByIdAsync(integrationId)
                ?? throw new ArgumentException("Integration không tồn tại");

            var brand = await _brandRepository.GetByIdAsync(integration.BrandId)
                ?? throw new ArgumentException("Thương hiệu không tồn tại");
            if (brand.UserId != requesterId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền");
            }

            var provider = ResolveProvider(integration.Platform);
            await provider.RefreshTokenIfNeededAsync(integration.SocialAccount);
            return true;
        }

        private IProviderService ResolveProvider(SocialPlatformEnum platform)
        {
            var name = platform.ToString().ToLower();
            var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                throw new InvalidOperationException($"Nhà cung cấp '{name}' không được hỗ trợ");
            }
            return provider;
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
    }
}