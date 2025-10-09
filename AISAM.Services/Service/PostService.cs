using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using System.Net;

namespace AISAM.Services.Service
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IContentRepository _contentRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;

        public PostService(IPostRepository postRepository, IContentRepository contentRepository, ISocialIntegrationRepository socialIntegrationRepository)
        {
            _postRepository = postRepository;
            _contentRepository = contentRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
        }

        public async Task<PagedResult<PostResponseDto>> GetPagedByUserIdAsync(Guid userId, PaginationRequest request)
        {
            var pagedPosts = await _postRepository.GetPagedByUserIdAsync(userId, request);

            var responseDtos = pagedPosts.Data.Select(post => new PostResponseDto
            {
                Id = post.Id,
                ContentId = post.ContentId,
                IntegrationId = post.IntegrationId,
                ExternalPostId = post.ExternalPostId,
                PublishedAt = post.PublishedAt,
                Status = post.Status,
                IsDeleted = post.IsDeleted,
                CreatedAt = post.CreatedAt,
                Content = post.Content != null ? new ContentInfoDto
                {
                    Id = post.Content.Id,
                    Title = post.Content.Title,
                    TextContent = post.Content.TextContent,
                    AdType = post.Content.AdType,
                    Status = post.Content.Status,
                    BrandName = post.Content.Brand?.Name
                } : null,
                Integration = post.Integration != null ? new IntegrationInfoDto
                {
                    Id = post.Integration.Id,
                    PlatformName = post.Integration.Platform.ToString(),
                    AccountName = post.Integration.SocialAccount?.AccountId ?? string.Empty,
                    IsActive = post.Integration.IsActive
                } : null
            }).ToList();

            return new PagedResult<PostResponseDto>
            {
                Data = responseDtos,
                TotalCount = pagedPosts.TotalCount,
                Page = pagedPosts.Page,
                PageSize = pagedPosts.PageSize
            };
        }

        public async Task<GenericResponse<Post>> CreatePostAsync(CreatePostRequest request)
        {
            try
            {
                // Validate ContentId exists
                var content = await _contentRepository.GetByIdAsync(request.ContentId);
                if (content == null)
                {
                    return GenericResponse<Post>.CreateError("Nội dung không tồn tại", HttpStatusCode.BadRequest);
                }

                // Validate IntegrationId exists
                var integration = await _socialIntegrationRepository.GetByIdAsync(request.IntegrationId);
                if (integration == null)
                {
                    return GenericResponse<Post>.CreateError("Tích hợp mạng xã hội không tồn tại", HttpStatusCode.BadRequest);
                }

                var post = new Post
                {
                    ContentId = request.ContentId,
                    IntegrationId = request.IntegrationId,
                    ExternalPostId = request.ExternalPostId,
                    PublishedAt = request.PublishedAt,
                    Status = ContentStatusEnum.Published,
                    CreatedAt = DateTime.UtcNow
                };

                var createdPost = await _postRepository.CreateAsync(post);
                return GenericResponse<Post>.CreateSuccess(createdPost, "Tạo bài viết thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<Post>.CreateError($"Lỗi khi tạo bài viết: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<Post>> GetPostByIdAsync(Guid id)
        {
            try
            {
                var post = await _postRepository.GetByIdAsync(id);
                if (post == null)
                {
                    return GenericResponse<Post>.CreateError("Bài viết không tồn tại", HttpStatusCode.NotFound);
                }

                return GenericResponse<Post>.CreateSuccess(post, "Lấy bài viết thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<Post>.CreateError($"Lỗi khi lấy bài viết: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<IEnumerable<Post>>> GetAllPostsAsync()
        {
            try
            {
                var posts = await _postRepository.GetAllAsync();
                return GenericResponse<IEnumerable<Post>>.CreateSuccess(posts, "Lấy tất cả bài viết thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<Post>>.CreateError($"Lỗi khi lấy tất cả bài viết: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<Post>> UpdatePostAsync(Guid id, UpdatePostRequest request)
        {
            try
            {
                var existingPost = await _postRepository.GetByIdAsync(id);
                if (existingPost == null)
                {
                    return GenericResponse<Post>.CreateError("Bài viết không tồn tại", HttpStatusCode.NotFound);
                }

                existingPost.ContentId = request.ContentId;
                existingPost.IntegrationId = request.IntegrationId;
                existingPost.ExternalPostId = request.ExternalPostId;
                existingPost.PublishedAt = request.PublishedAt;

                var updatedPost = await _postRepository.UpdateAsync(existingPost);
                return GenericResponse<Post>.CreateSuccess(updatedPost, "Cập nhật bài viết thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<Post>.CreateError($"Lỗi khi cập nhật bài viết: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<bool>> DeletePostAsync(Guid id)
        {
            try
            {
                var result = await _postRepository.DeleteAsync(id);
                if (!result)
                {
                    return GenericResponse<bool>.CreateError("Bài viết không tồn tại", HttpStatusCode.NotFound);
                }

                return GenericResponse<bool>.CreateSuccess(true, "Xóa bài viết thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi xóa bài viết: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<bool>> SoftDeletePostAsync(Guid id)
        {
            try
            {
                var result = await _postRepository.SoftDeleteAsync(id);
                if (!result)
                {
                    return GenericResponse<bool>.CreateError("Bài viết không tồn tại", HttpStatusCode.NotFound);
                }

                return GenericResponse<bool>.CreateSuccess(true, "Bài viết đã được xóa mềm thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi xóa mềm bài viết: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<bool>> RestorePostAsync(Guid id)
        {
            try
            {
                var result = await _postRepository.RestoreAsync(id);
                if (!result)
                {
                    return GenericResponse<bool>.CreateError("Bài viết không tồn tại", HttpStatusCode.NotFound);
                }

                return GenericResponse<bool>.CreateSuccess(true, "Bài viết đã được khôi phục thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi khôi phục bài viết: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        // Methods for tracking published posts
        public async Task<GenericResponse<IEnumerable<Post>>> GetPostsByContentIdAsync(Guid contentId)
        {
            try
            {
                var posts = await _postRepository.GetPostsByContentIdAsync(contentId);
                return GenericResponse<IEnumerable<Post>>.CreateSuccess(posts, "Lấy bài viết theo ID nội dung thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<Post>>.CreateError($"Lỗi khi lấy bài viết theo ID nội dung: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<IEnumerable<Post>>> GetPostsByIntegrationIdAsync(Guid integrationId)
        {
            try
            {
                var posts = await _postRepository.GetPostsByIntegrationIdAsync(integrationId);
                return GenericResponse<IEnumerable<Post>>.CreateSuccess(posts, "Lấy bài viết theo ID tích hợp thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<Post>>.CreateError($"Lỗi khi lấy bài viết theo ID tích hợp: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<Post>> GetPostByExternalIdAsync(string externalPostId)
        {
            try
            {
                var post = await _postRepository.GetByExternalPostIdAsync(externalPostId);
                if (post == null)
                {
                    return GenericResponse<Post>.CreateError("Bài viết không tồn tại", HttpStatusCode.NotFound);
                }

                return GenericResponse<Post>.CreateSuccess(post, "Lấy bài viết thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<Post>.CreateError($"Lỗi khi lấy bài viết theo ID bên ngoài: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<IEnumerable<Post>>> GetPublishedPostsAsync()
        {
            try
            {
                var posts = await _postRepository.GetPublishedPostsAsync();
                return GenericResponse<IEnumerable<Post>>.CreateSuccess(posts, "Lấy bài viết đã xuất bản thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<Post>>.CreateError($"Lỗi khi lấy bài viết đã xuất bản: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<IEnumerable<Post>>> GetPostsPublishedBetweenAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var posts = await _postRepository.GetPostsPublishedBetweenAsync(startDate, endDate);
                return GenericResponse<IEnumerable<Post>>.CreateSuccess(posts, "Lấy bài viết theo khoảng thời gian xuất bản thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<Post>>.CreateError($"Lỗi khi lấy bài viết theo khoảng thời gian xuất bản: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
    }
}