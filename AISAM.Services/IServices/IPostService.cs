using AISAM.Common.Dtos;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;

namespace AISAM.Services.IServices
{
    public interface IPostService
    {
        Task<PublishResultDto> PublishPostAsync(Guid userId, Guid contentId, IEnumerable<Guid> integrationIds);
        Task<Guid> SchedulePostAsync(Guid userId, Guid contentId, Guid integrationId, DateTime scheduledAtUtc);
        Task<PagedResult<PostListItemDto>> GetPostsAsync(Guid requesterId, Guid? brandId, int page, int pageSize, ContentStatusEnum? status = null);
        Task<bool> DeleteOrCancelPostAsync(Guid requesterId, Guid postId, bool isAdminOverride = false);
        Task<bool> PullReportsAsync(Guid requesterId, Guid postId);
        Task<bool> ReAuthIntegrationAsync(Guid requesterId, Guid integrationId);
    }

    public class PostListItemDto
    {
        public Guid Id { get; set; }
        public Guid ContentId { get; set; }
        public Guid IntegrationId { get; set; }
        public string? ExternalPostId { get; set; }
        public DateTime PublishedAt { get; set; }
        public ContentStatusEnum Status { get; set; }
        public bool IsDeleted { get; set; }
    }
}

