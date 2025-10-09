using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface IPostService
    {
        Task<PagedResult<PostResponseDto>> GetPagedByUserIdAsync(Guid userId, PaginationRequest request);
        Task<GenericResponse<Post>> CreatePostAsync(CreatePostRequest request);
        Task<GenericResponse<Post>> GetPostByIdAsync(Guid id);
        Task<GenericResponse<IEnumerable<Post>>> GetAllPostsAsync();
        Task<GenericResponse<Post>> UpdatePostAsync(Guid id, UpdatePostRequest request);
        Task<GenericResponse<bool>> DeletePostAsync(Guid id);
        Task<GenericResponse<bool>> SoftDeletePostAsync(Guid id);
        Task<GenericResponse<bool>> RestorePostAsync(Guid id);
        // Methods for tracking published posts
        Task<GenericResponse<IEnumerable<Post>>> GetPostsByContentIdAsync(Guid contentId);
        Task<GenericResponse<IEnumerable<Post>>> GetPostsByIntegrationIdAsync(Guid integrationId);
        Task<GenericResponse<Post>> GetPostByExternalIdAsync(string externalPostId);
        Task<GenericResponse<IEnumerable<Post>>> GetPublishedPostsAsync();
        Task<GenericResponse<IEnumerable<Post>>> GetPostsPublishedBetweenAsync(DateTime startDate, DateTime endDate);
    }
}