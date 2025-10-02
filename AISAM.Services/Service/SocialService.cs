using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AISAM.Data.Model;

namespace AISAM.Services.Service
{
    public class SocialService : ISocialService
    {
        private readonly ISocialAccountRepository _socialAccountRepository;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SocialService> _logger;
        private readonly Dictionary<string, IProviderService> _providers;
        private readonly FacebookSettings _facebookSettings;

        public SocialService(
            ISocialAccountRepository socialAccountRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IUserRepository userRepository,
            ILogger<SocialService> logger,
            IEnumerable<IProviderService> providers,
            IOptions<FacebookSettings> facebookSettings)
        {
            _socialAccountRepository = socialAccountRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _userRepository = userRepository;
            _logger = logger;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p);
            _facebookSettings = facebookSettings.Value;
        }

        public async Task<AuthUrlResponse> GetAuthUrlAsync(string provider, string? state = null, Guid? userId = null)
        {
            if (!_providers.TryGetValue(provider, out var providerService))
            {
                throw new ArgumentException($"Provider '{provider}' is not supported");
            }

            var redirectUri = GetRedirectUri(provider);
            if (userId.HasValue)
            {
                redirectUri = AppendUserIdQuery(redirectUri, userId.Value);
            }
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
            // Must be IDENTICAL to the redirect_uri used in the OAuth dialog (including userId param)
            redirectUri = AppendUserIdQuery(redirectUri, request.UserId);
            var accountData = await providerService.ExchangeCodeAsync(request.Code, redirectUri);

            // Check if account already exists
            var platform = ParseProviderToEnum(request.Provider);
            var existingAccount = await _socialAccountRepository.GetByPlatformAndAccountIdAsync(platform, accountData.ProviderUserId);
            if (existingAccount != null)
            {
                throw new InvalidOperationException("This social account is already linked");
            }

            // Create new social account only (opt-in pages later)
            var socialAccount = new SocialAccount
            {
                UserId = request.UserId,
                Platform = platform,
                AccountId = accountData.ProviderUserId,
                UserAccessToken = accountData.AccessToken,
                ExpiresAt = accountData.ExpiresAt,
                IsActive = true
            };

            await _socialAccountRepository.CreateAsync(socialAccount);

            return MapToDto(socialAccount);
        }

        public async Task<bool> UnlinkAccountAsync(Guid userId, Guid socialAccountId)
        {
            try
            {
                var account = await _socialAccountRepository.GetByIdAsync(socialAccountId);
                if (account == null || account.UserId != userId)
                {
                    return false;
                }

                await _socialAccountRepository.DeleteAsync(socialAccountId);
                _logger.LogInformation("Successfully unlinked social account {SocialAccountId} for user {UserId}", 
                    socialAccountId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking social account {SocialAccountId} for user {UserId}", 
                    socialAccountId, userId);
                throw;
            }
        }

        public async Task<IEnumerable<SocialAccountDto>> GetUserAccountsAsync(Guid userId)
        {
            var accounts = await _socialAccountRepository.GetByUserIdAsync(userId);
            return accounts.Select(MapToDto);
        }

        public async Task<IEnumerable<SocialTargetDto>> GetAccountTargetsAsync(Guid socialAccountId)
        {
            var integrations = await _socialIntegrationRepository.GetBySocialAccountIdAsync(socialAccountId);
            return integrations.Select(MapToDtoFromIntegration);
        }

        public async Task<IEnumerable<SocialTargetDto>> ListAvailableTargetsAsync(Guid userId, string provider)
        {
            if (!_providers.TryGetValue(provider, out var providerService))
            {
                throw new ArgumentException($"Provider '{provider}' is not supported");
            }

            var platform = ParseProviderToEnum(provider);
            var account = await _socialAccountRepository.GetByUserIdAndPlatformAsync(userId, platform);
            if (account == null)
            {
                throw new InvalidOperationException($"No linked {provider} account for this user");
            }

            var available = await providerService.GetTargetsAsync(account.UserAccessToken);
            return available;
        }

        public async Task<SocialAccountDto> LinkSelectedTargetsAsync(Guid userId, string provider, IEnumerable<string> providerTargetIds)
        {
            if (!_providers.TryGetValue(provider, out var providerService))
            {
                throw new ArgumentException($"Provider '{provider}' is not supported");
            }

            var platform = ParseProviderToEnum(provider);
            var account = await _socialAccountRepository.GetByUserIdAndPlatformAsync(userId, platform);
            if (account == null)
            {
                throw new InvalidOperationException($"No linked {provider} account for this user");
            }

            var available = (await providerService.GetTargetsAsync(account.UserAccessToken)).ToList();
            var selectedSet = new HashSet<string>(providerTargetIds);
            var selected = available.Where(t => selectedSet.Contains(t.ProviderTargetId));

            // Do not fetch/store per-target tokens during linking; we'll lazy-fetch at publish time

            foreach (var targetDto in selected)
            {
                var existingIntegration = await _socialIntegrationRepository.GetByExternalIdAsync(targetDto.ProviderTargetId);
                if (existingIntegration != null && existingIntegration.SocialAccountId == account.Id)
                {
                    continue; // already linked
                }

                var integration = new SocialIntegration
                {
                    UserId = userId,
                    BrandId = Guid.NewGuid(), // TODO: This should come from request or default brand
                    SocialAccountId = account.Id,
                    Platform = platform,
                    AccessToken = null, // Will be fetched later
                    ExternalId = targetDto.ProviderTargetId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _socialIntegrationRepository.CreateAsync(integration);
            }

            var reloaded = await _socialAccountRepository.GetByIdWithIntegrationsAsync(account.Id);
            return MapToDto(reloaded);
        }

        public async Task<bool> UnlinkTargetAsync(Guid userId, Guid socialIntegrationId)
        {
            try
            {
                var integration = await _socialIntegrationRepository.GetByIdAsync(socialIntegrationId);
                if (integration == null || integration.UserId != userId)
                {
                    return false;
                }

                await _socialIntegrationRepository.DeleteAsync(socialIntegrationId);
                _logger.LogInformation("Successfully unlinked social integration {IntegrationId} for user {UserId}",
                    socialIntegrationId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking social integration {IntegrationId} for user {UserId}",
                    socialIntegrationId, userId);
                throw;
            }
        }
        
        private string GetRedirectUri(string provider)
        {
            // Use configured redirect URI from settings
            if (provider == "facebook")
            {
                // For Facebook, use the configured redirect URI from FacebookSettings
                return _facebookSettings.RedirectUri;
            }
            
            // For other providers, use the default pattern
            return $"http://localhost:5000/auth/{provider}/callback";
        }

        private string AppendUserIdQuery(string uri, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(uri)) return uri;
            var separator = uri.Contains("?") ? "&" : "?";
            return $"{uri}{separator}userId={Uri.EscapeDataString(userId.ToString())}";
        }

        private SocialPlatformEnum ParseProviderToEnum(string provider)
        {
            return provider.ToLower() switch
            {
                "facebook" => SocialPlatformEnum.Facebook,
                "instagram" => SocialPlatformEnum.Instagram,
                "tiktok" => SocialPlatformEnum.TikTok,
                "twitter" => SocialPlatformEnum.Twitter,
                _ => throw new ArgumentException($"Unsupported provider: {provider}")
            };
        }

        private SocialAccountDto MapToDto(SocialAccount account)
        {
            return new SocialAccountDto
            {
                Id = account.Id,
                Provider = account.Platform.ToString().ToLower(),
                ProviderUserId = account.AccountId ?? string.Empty,
                AccessToken = account.UserAccessToken,
                IsActive = account.IsActive,
                ExpiresAt = account.ExpiresAt,
                CreatedAt = account.CreatedAt,
                Targets = account.SocialIntegrations?.Select(MapToDtoFromIntegration).ToList() ?? new List<SocialTargetDto>()
            };
        }

        private SocialTargetDto MapToDtoFromIntegration(SocialIntegration integration)
        {
            return new SocialTargetDto
            {
                Id = integration.Id,
                ProviderTargetId = integration.ExternalId ?? string.Empty,
                Name = $"Page {integration.ExternalId}", // Use ExternalId as name since it's usually the page ID
                Type = integration.Platform.ToString().ToLower(), // Use platform as type
                Category = null, // SocialIntegration doesn't have category
                ProfilePictureUrl = null, // SocialIntegration doesn't have profile picture URL
                IsActive = integration.IsActive
            };
        }

    }
}