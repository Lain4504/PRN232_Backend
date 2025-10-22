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
        private readonly IProfileRepository _profileRepository;
        private readonly ILogger<SocialService> _logger;
        private readonly Dictionary<string, IProviderService> _providers;
        private readonly FacebookSettings _facebookSettings;
        private readonly FrontendSettings _frontendSettings;

        public SocialService(
            ISocialAccountRepository socialAccountRepository,
            ISocialIntegrationRepository socialIntegrationRepository,
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            ILogger<SocialService> logger,
            IEnumerable<IProviderService> providers,
            IOptions<FacebookSettings> facebookSettings,
            IOptions<FrontendSettings> frontendSettings)
        {
            _socialAccountRepository = socialAccountRepository;
            _socialIntegrationRepository = socialIntegrationRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _logger = logger;
            _providers = providers.ToDictionary(p => p.ProviderName, p => p);
            _facebookSettings = facebookSettings.Value;
            _frontendSettings = frontendSettings.Value;
        }

        public async Task<AuthUrlResponse> GetAuthUrlAsync(string provider, string? state = null, Guid? userId = null)
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

            // Verify profile exists
            var profile = await _profileRepository.GetByIdAsync(request.ProfileId);
            if (profile == null)
            {
                throw new ArgumentException("Profile not found");
            }

            var redirectUri = GetRedirectUri(request.Provider);
            // Must be IDENTICAL to the redirect_uri used in the OAuth dialog
            var accountData = await providerService.ExchangeCodeAsync(request.Code, redirectUri);

            // Check if this specific Facebook account is already linked to this profile
            var platform = ParseProviderToEnum(request.Provider);
            var existingAccount = await _socialAccountRepository.GetByProfileIdPlatformAndAccountIdAsync(request.ProfileId, platform, accountData.ProviderUserId);
            if (existingAccount != null)
            {
                throw new InvalidOperationException("Tài khoản mạng xã hội này đã được liên kết với profile này");
            }

            // Create new social account only (opt-in pages later)
            var socialAccount = new SocialAccount
            {
                ProfileId = request.ProfileId,
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
                if (account == null || account.ProfileId != userId)
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

        public async Task<IEnumerable<SocialAccountDto>> GetProfileAccountsAsync(Guid profileId)
        {
            var accounts = await _socialAccountRepository.GetByProfileIdAsync(profileId);
            return accounts.Select(MapToDto);
        }

        public async Task<IEnumerable<SocialTargetDto>> GetAccountTargetsAsync(Guid socialAccountId)
        {
            var integrations = await _socialIntegrationRepository.GetBySocialAccountIdAsync(socialAccountId);
            return integrations.Select(MapToDtoFromIntegration);
        }

        public async Task<IEnumerable<AvailableTargetDto>> ListAvailableTargetsForAccountAsync(Guid socialAccountId)
        {
            var account = await _socialAccountRepository.GetByIdAsync(socialAccountId);
            if (account == null)
            {
                throw new ArgumentException("Social account not found");
            }

            if (!_providers.TryGetValue(account.Platform.ToString().ToLower(), out var providerService))
            {
                throw new ArgumentException($"Provider '{account.Platform}' is not supported");
            }

            var available = await providerService.GetTargetsAsync(account.UserAccessToken);
            return available;
        }

        public async Task<SocialAccountDto?> GetSocialAccountByIdAsync(Guid socialAccountId)
        {
            var account = await _socialAccountRepository.GetByIdAsync(socialAccountId);
            return account != null ? MapToDto(account) : null;
        }
        
        public async Task<SocialAccountDto> LinkSelectedTargetsForAccountAsync(Guid socialAccountId, LinkSelectedTargetsRequest request)
        {
            if (!_providers.TryGetValue(request.Provider, out var providerService))
            {
                throw new ArgumentException($"Provider '{request.Provider}' is not supported");
            }

            var platform = ParseProviderToEnum(request.Provider);
             
            var account = await _socialAccountRepository.GetByIdAsync(socialAccountId);
            if (account == null)
            {
                throw new ArgumentException("Social account not found");
            }
            
            // Verify account belongs to the correct platform
            if (account.Platform != platform)
            {
                throw new ArgumentException("Social account platform mismatch");
            }
            //TO DO: Verify brand belongs to user when brand repository is ready
            // Verify brand exists and belongs to user
            //var brandId = _brandRepository.findyByIdAndUserId(request.BrandId, account.ProfileId);
            var brandId = request.BrandId;
            if (brandId == null)
            {
                throw new ArgumentException("Brand not found or does not belong to user");
            }
            try
            {
                var available = (await providerService.GetTargetsAsync(account.UserAccessToken)).ToList();
                var selectedSet = new HashSet<string>(request.ProviderTargetIds);
                var selected = available.Where(t => selectedSet.Contains(t.ProviderTargetId));

                // Get page access tokens for selected targets
                var targetAccessTokens = await providerService.GetTargetAccessTokensAsync(
                    account.UserAccessToken,
                    selected.Select(t => t.ProviderTargetId));

                foreach (var targetDto in selected)
                {
                    var existingIntegration =
                        await _socialIntegrationRepository.GetByExternalIdAsync(targetDto.ProviderTargetId);
                    if (existingIntegration != null && existingIntegration.SocialAccountId == account.Id)
                    {
                        continue; // already linked
                    }

                    // Get page access token for this target
                    if (!targetAccessTokens.TryGetValue(targetDto.ProviderTargetId, out var pageAccessToken))
                    {
                        throw new InvalidOperationException(
                            $"Could not get access token for page {targetDto.ProviderTargetId}");
                    }

                    var integration = new SocialIntegration
                    {
                        ProfileId = account.ProfileId,
                        BrandId = brandId,
                        SocialAccountId = account.Id,
                        Platform = account.Platform,
                        AccessToken = pageAccessToken, // Page access token from Facebook
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
            catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error linking targets to social account {SocialAccountId}", socialAccountId);
                throw new ArgumentException("Error linking targets to social account " + ex.Message, ex);
            }
        }

        public async Task<bool> UnlinkTargetAsync(Guid userId, Guid socialIntegrationId)
        {
            try
            {
                var integration = await _socialIntegrationRepository.GetByIdAsync(socialIntegrationId);
                if (integration == null || integration.ProfileId != userId)
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
        
        public async Task<IEnumerable<AdAccountDto>> GetAdAccountsAsync(Guid socialAccountId)
        {
            var account = await _socialAccountRepository.GetByIdAsync(socialAccountId);
            if (account == null)
            {
                throw new ArgumentException("Social account not found");
            }

            if (!_providers.TryGetValue(account.Platform.ToString().ToLower(), out var providerService))
            {
                throw new ArgumentException($"Provider '{account.Platform}' is not supported");
            }

            if (providerService is not FacebookProvider facebookProvider)
            {
                throw new ArgumentException("Ad accounts are only available for Facebook");
            }

            return await facebookProvider.GetAdAccountsAsync(account.UserAccessToken);
        }

        public async Task<bool> LinkAdAccountToIntegrationAsync(Guid socialIntegrationId, string adAccountId)
        {
            var integration = await _socialIntegrationRepository.GetByIdAsync(socialIntegrationId);
            if (integration == null) return false;
    
            integration.AdAccountId = adAccountId;
            integration.UpdatedAt = DateTime.UtcNow;
    
            await _socialIntegrationRepository.UpdateAsync(integration);
            return true;
        }
        
        private string GetRedirectUri(string provider)
        {
            // Redirect to frontend callback URL from configuration
            return $"{_frontendSettings.BaseUrl}/social-callback/{provider}";
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
                ProfileId = account.ProfileId,
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

        public async Task<IEnumerable<SocialIntegrationDto>> GetSocialIntegrationsByBrandIdAsync(Guid brandId, Guid profileId)
        {
            try
            {
                // Verify profile exists
                var profile = await _profileRepository.GetByIdAsync(profileId);
                if (profile == null)
                {
                    throw new UnauthorizedAccessException("Profile not found");
                }

                // Get social integrations for the brand
                var integrations = await _socialIntegrationRepository.GetByBrandIdAsync(brandId);
                if (integrations == null)
                {
                    return new List<SocialIntegrationDto>();
                }

                // Convert to DTO
                return new List<SocialIntegrationDto>
                {
                    new SocialIntegrationDto
                    {
                        Id = integrations.Id,
                        SocialAccountId = integrations.SocialAccountId,
                        ProfileId = integrations.ProfileId,
                        BrandId = integrations.BrandId,
                        ExternalId = integrations.ExternalId ?? string.Empty,
                        Name = integrations.ExternalId ?? "Social Integration", // Use ExternalId as name
                        Platform = integrations.Platform.ToString(),
                        IsActive = integrations.IsActive,
                        CreatedAt = integrations.CreatedAt,
                        UpdatedAt = integrations.UpdatedAt,
                        SocialAccountName = integrations.SocialAccount?.AccountId,
                        BrandName = integrations.Brand?.Name,
                        ProfileName = integrations.Profile?.Name
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting social integrations for brand {BrandId}", brandId);
                throw;
            }
        }

    }
}