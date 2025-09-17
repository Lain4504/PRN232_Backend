using BookStore.Common.Models;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using BookStore.Services.IServices;
using Microsoft.Extensions.Logging;

namespace BookStore.Services.Service
{
    public class SocialService : ISocialService
    {
        private readonly ISocialAccountRepository _socialAccountRepository;
        private readonly ISocialTargetRepository _socialTargetRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SocialService> _logger;
        private readonly Dictionary<string, IProviderService> _providers;

        public SocialService(
            ISocialAccountRepository socialAccountRepository,
            ISocialTargetRepository socialTargetRepository,
            IUserRepository userRepository,
            ILogger<SocialService> logger,
            IEnumerable<IProviderService> providers)
        {
            _socialAccountRepository = socialAccountRepository;
            _socialTargetRepository = socialTargetRepository;
            _userRepository = userRepository;
            _logger = logger;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p);
        }

        public async Task<AuthUrlResponse> GetAuthUrlAsync(string provider, string? state = null)
        {
            if (!_providers.TryGetValue(provider, out var providerService))
            {
                throw new ArgumentException($"Provider '{provider}' is not supported");
            }

            var redirectUri = GetRedirectUri(provider);
            var actualState = state ?? Guid.NewGuid().ToString();
            var authUrl = await providerService.GetAuthUrlAsync(actualState, redirectUri);

            return new AuthUrlResponse
            {
                AuthUrl = authUrl,
                State = actualState
            };
        }

        public async Task<SocialAccountDto> LinkAccountAsync(LinkSocialAccountRequest request)
        {
            if (!_providers.TryGetValue(request.Provider, out var providerService))
            {
                throw new ArgumentException($"Provider '{request.Provider}' is not supported");
            }

            // Verify user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            var redirectUri = GetRedirectUri(request.Provider);
            var accountData = await providerService.ExchangeCodeAsync(request.Code, redirectUri);

            // Check if account already exists
            var existingAccount = await _socialAccountRepository.GetByProviderAndUserIdAsync(request.Provider, accountData.ProviderUserId);
            if (existingAccount != null)
            {
                throw new InvalidOperationException("This social account is already linked");
            }

            // Create new social account
            var socialAccount = new SocialAccount
            {
                UserId = request.UserId,
                Provider = request.Provider,
                ProviderUserId = accountData.ProviderUserId,
                AccessToken = "encrypted_" + Guid.NewGuid().ToString(), // TODO: Implement proper encryption
                ExpiresAt = accountData.ExpiresAt,
                IsActive = true
            };

            await _socialAccountRepository.CreateAsync(socialAccount);

            // Get and create targets
            var targets = await providerService.GetTargetsAsync(socialAccount.AccessToken);
            foreach (var targetDto in targets)
            {
                var target = new SocialTarget
                {
                    SocialAccountId = socialAccount.Id,
                    ProviderTargetId = targetDto.ProviderTargetId,
                    Name = targetDto.Name,
                    Type = targetDto.Type,
                    Category = targetDto.Category,
                    ProfilePictureUrl = targetDto.ProfilePictureUrl,
                    IsActive = targetDto.IsActive
                };

                await _socialTargetRepository.CreateAsync(target);
            }

            return MapToDto(socialAccount);
        }

        public async Task<bool> UnlinkAccountAsync(int userId, int socialAccountId)
        {
            var account = await _socialAccountRepository.GetByIdAsync(socialAccountId);
            if (account == null || account.UserId != userId)
            {
                return false;
            }

            await _socialAccountRepository.DeleteAsync(socialAccountId);
            return true;
        }

        public async Task<IEnumerable<SocialAccountDto>> GetUserAccountsAsync(int userId)
        {
            var accounts = await _socialAccountRepository.GetByUserIdAsync(userId);
            return accounts.Select(MapToDto);
        }

        public async Task<IEnumerable<SocialTargetDto>> GetAccountTargetsAsync(int socialAccountId)
        {
            var targets = await _socialTargetRepository.GetBySocialAccountIdAsync(socialAccountId);
            return targets.Select(MapToDto);
        }

        public async Task<IEnumerable<SocialTargetDto>> SyncAccountTargetsAsync(int socialAccountId)
        {
            var account = await _socialAccountRepository.GetByIdAsync(socialAccountId);
            if (account == null)
            {
                throw new ArgumentException("Social account not found");
            }

            if (!_providers.TryGetValue(account.Provider, out var providerService))
            {
                throw new ArgumentException($"Provider '{account.Provider}' is not supported");
            }

            // Get current targets from provider
            var providerTargets = await providerService.GetTargetsAsync(account.AccessToken);
            
            // Get existing targets
            var existingTargets = await _socialTargetRepository.GetBySocialAccountIdAsync(socialAccountId);
            var existingTargetIds = existingTargets.Select(t => t.ProviderTargetId).ToHashSet();

            // Add new targets
            foreach (var targetDto in providerTargets)
            {
                if (!existingTargetIds.Contains(targetDto.ProviderTargetId))
                {
                    var target = new SocialTarget
                    {
                        SocialAccountId = socialAccountId,
                        ProviderTargetId = targetDto.ProviderTargetId,
                        Name = targetDto.Name,
                        Type = targetDto.Type,
                        Category = targetDto.Category,
                        ProfilePictureUrl = targetDto.ProfilePictureUrl,
                        IsActive = targetDto.IsActive
                    };

                    await _socialTargetRepository.CreateAsync(target);
                }
            }

            // Mark missing targets as inactive
            var providerTargetIds = providerTargets.Select(t => t.ProviderTargetId).ToHashSet();
            foreach (var existingTarget in existingTargets)
            {
                if (!providerTargetIds.Contains(existingTarget.ProviderTargetId) && existingTarget.IsActive)
                {
                    existingTarget.IsActive = false;
                    await _socialTargetRepository.UpdateAsync(existingTarget);
                }
            }

            return providerTargets;
        }

        private string GetRedirectUri(string provider)
        {
            // TODO: Make this configurable
            return $"http://localhost:5000/auth/{provider}/callback";
        }

        private SocialAccountDto MapToDto(SocialAccount account)
        {
            return new SocialAccountDto
            {
                Id = account.Id,
                Provider = account.Provider,
                ProviderUserId = account.ProviderUserId,
                IsActive = account.IsActive,
                ExpiresAt = account.ExpiresAt,
                CreatedAt = account.CreatedAt,
                Targets = account.SocialTargets?.Select(MapToDto).ToList() ?? new List<SocialTargetDto>()
            };
        }

        private SocialTargetDto MapToDto(SocialTarget target)
        {
            return new SocialTargetDto
            {
                Id = target.Id,
                ProviderTargetId = target.ProviderTargetId,
                Name = target.Name,
                Type = target.Type,
                Category = target.Category,
                ProfilePictureUrl = target.ProfilePictureUrl,
                IsActive = target.IsActive
            };
        }
    }
}