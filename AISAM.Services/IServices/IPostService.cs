using AISAM.Common.Dtos;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;

namespace AISAM.Services.IServices
{
    public interface IPostService
    {
        Task<IEnumerable<PostListItemDto>> GetAllPostsAsync();
        Task<PagedResult<PostListItemDto>> GetPostsAsync(Guid requesterId, Guid? brandId, int page, int pageSize, ContentStatusEnum? status = null);
        Task<PostListItemDto?> GetPostByIdAsync(Guid requesterId, Guid postId);
        Task<bool> DeletePostAsync(Guid requesterId, Guid postId, bool isAdminOverride = false);
    }
}