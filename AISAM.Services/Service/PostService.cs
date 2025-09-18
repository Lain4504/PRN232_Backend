using BookStore.Common.Models;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using BookStore.Services.IServices;
using Microsoft.Extensions.Logging;

namespace BookStore.Services.Service
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly ISocialAccountRepository _socialAccountRepository;
        private readonly ISocialTargetRepository _socialTargetRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<PostService> _logger;
        private readonly Dictionary<string, IProviderService> _providers;

        public PostService(
            IPostRepository postRepository,
            ISocialAccountRepository socialAccountRepository,
            ISocialTargetRepository socialTargetRepository,
            IUserRepository userRepository,
            ILogger<PostService> logger,
            IEnumerable<IProviderService> providers)
        {
            _postRepository = postRepository;
            _socialAccountRepository = socialAccountRepository;
            _socialTargetRepository = socialTargetRepository;
            _userRepository = userRepository;
            _logger = logger;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p);
        }

        public async Task<PostResponseDto> CreatePostAsync(CreatePostRequest request)
        {
            // Validate entities exist
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new ArgumentException("User not found");

            var socialAccount = await _socialAccountRepository.GetByIdAsync(request.SocialAccountId);
            if (socialAccount == null || socialAccount.UserId != request.UserId)
                throw new ArgumentException("Social account not found or doesn't belong to user");

            var socialTarget = await _socialTargetRepository.GetByIdAsync(request.SocialTargetId);
            if (socialTarget == null || socialTarget.SocialAccountId != request.SocialAccountId)
                throw new ArgumentException("Social target not found or doesn't belong to social account");

            // Create post entity
            var post = new Post
            {
                UserId = request.UserId,
                SocialAccountId = request.SocialAccountId,
                SocialTargetId = request.SocialTargetId,
                Provider = socialAccount.Provider,
                Message = request.Message,
                LinkUrl = request.LinkUrl,
                ImageUrl = request.ImageUrl,
                Status = request.Published ? PostStatus.Posted : PostStatus.Draft
            };

            // If publishing immediately, call provider
            if (request.Published)
            {
                var publishResult = await PublishToProviderAsync(socialAccount, socialTarget, post);
                if (publishResult.Success)
                {
                    post.ProviderPostId = publishResult.ProviderPostId;
                    post.PostedAt = publishResult.PostedAt;
                    post.Status = PostStatus.Posted;
                }
                else
                {
                    post.Status = PostStatus.Failed;
                    post.ErrorMessage = publishResult.ErrorMessage;
                }
            }

            await _postRepository.CreateAsync(post);
            return MapToDto(post);
        }

        public async Task<PostResponseDto> SchedulePostAsync(SchedulePostRequest request)
        {
            // Validate entities exist
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new ArgumentException("User not found");

            var socialAccount = await _socialAccountRepository.GetByIdAsync(request.SocialAccountId);
            if (socialAccount == null || socialAccount.UserId != request.UserId)
                throw new ArgumentException("Social account not found or doesn't belong to user");

            var socialTarget = await _socialTargetRepository.GetByIdAsync(request.SocialTargetId);
            if (socialTarget == null || socialTarget.SocialAccountId != request.SocialAccountId)
                throw new ArgumentException("Social target not found or doesn't belong to social account");

            if (request.ScheduledTime <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled time must be in the future");

            // Create scheduled post
            var post = new Post
            {
                UserId = request.UserId,
                SocialAccountId = request.SocialAccountId,
                SocialTargetId = request.SocialTargetId,
                Provider = socialAccount.Provider,
                Message = request.Message,
                LinkUrl = request.LinkUrl,
                ImageUrl = request.ImageUrl,
                ScheduledTime = request.ScheduledTime,
                Status = PostStatus.Scheduled
            };

            await _postRepository.CreateAsync(post);
            return MapToDto(post);
        }

        public async Task<PostResponseDto> PublishPostAsync(int postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
                throw new ArgumentException("Post not found");

            if (post.Status != PostStatus.Draft && post.Status != PostStatus.Failed)
                throw new InvalidOperationException("Only draft or failed posts can be published");

            var socialAccount = await _socialAccountRepository.GetByIdAsync(post.SocialAccountId!.Value);
            var socialTarget = await _socialTargetRepository.GetByIdAsync(post.SocialTargetId!.Value);

            if (socialAccount == null || socialTarget == null)
                throw new InvalidOperationException("Associated social account or target not found");

            var publishResult = await PublishToProviderAsync(socialAccount, socialTarget, post);
            
            if (publishResult.Success)
            {
                post.ProviderPostId = publishResult.ProviderPostId;
                post.PostedAt = publishResult.PostedAt;
                post.Status = PostStatus.Posted;
                post.ErrorMessage = null;
            }
            else
            {
                post.Status = PostStatus.Failed;
                post.ErrorMessage = publishResult.ErrorMessage;
            }

            await _postRepository.UpdateAsync(post);
            return MapToDto(post);
        }

        public async Task<IEnumerable<PostResponseDto>> GetUserPostsAsync(int userId)
        {
            var posts = await _postRepository.GetByUserIdAsync(userId);
            return posts.Select(MapToDto);
        }

        public async Task<IEnumerable<PostResponseDto>> GetTargetPostsAsync(int socialTargetId)
        {
            var posts = await _postRepository.GetBySocialTargetIdAsync(socialTargetId);
            return posts.Select(MapToDto);
        }

        public async Task<bool> DeletePostAsync(int postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
                return false;

            await _postRepository.DeleteAsync(postId);
            return true;
        }

        public async Task ProcessScheduledPostsAsync()
        {
            var scheduledPosts = await _postRepository.GetScheduledPostsAsync();
            
            foreach (var post in scheduledPosts)
            {
                try
                {
                    var socialAccount = await _socialAccountRepository.GetByIdAsync(post.SocialAccountId!.Value);
                    var socialTarget = await _socialTargetRepository.GetByIdAsync(post.SocialTargetId!.Value);

                    if (socialAccount == null || socialTarget == null)
                    {
                        post.Status = PostStatus.Failed;
                        post.ErrorMessage = "Associated social account or target not found";
                        await _postRepository.UpdateAsync(post);
                        continue;
                    }

                    var publishResult = await PublishToProviderAsync(socialAccount, socialTarget, post);
                    
                    if (publishResult.Success)
                    {
                        post.ProviderPostId = publishResult.ProviderPostId;
                        post.PostedAt = publishResult.PostedAt;
                        post.Status = PostStatus.Posted;
                        post.ErrorMessage = null;
                    }
                    else
                    {
                        post.Status = PostStatus.Failed;
                        post.ErrorMessage = publishResult.ErrorMessage;
                    }

                    await _postRepository.UpdateAsync(post);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing scheduled post {PostId}", post.Id);
                    post.Status = PostStatus.Failed;
                    post.ErrorMessage = ex.Message;
                    await _postRepository.UpdateAsync(post);
                }
            }
        }

        private async Task<PublishResultDto> PublishToProviderAsync(SocialAccount account, SocialTarget target, Post post)
        {
            if (!_providers.TryGetValue(account.Provider, out var provider))
            {
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = $"Provider '{account.Provider}' not found"
                };
            }

            var postDto = new PostDto
            {
                Message = post.Message,
                LinkUrl = post.LinkUrl,
                ImageUrl = post.ImageUrl,
                Metadata = post.Metadata
            };

            return await provider.PublishAsync(account, target, postDto);
        }

        private PostResponseDto MapToDto(Post post)
        {
            return new PostResponseDto
            {
                Id = post.Id,
                Provider = post.Provider ?? "",
                ProviderPostId = post.ProviderPostId,
                Message = post.Message,
                LinkUrl = post.LinkUrl,
                ImageUrl = post.ImageUrl,
                ScheduledTime = post.ScheduledTime,
                PostedAt = post.PostedAt,
                Status = post.Status.ToString(),
                ErrorMessage = post.ErrorMessage,
                CreatedAt = post.CreatedAt
            };
        }
    }
}