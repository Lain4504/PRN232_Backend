using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
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

        public async Task<SocialAccountDto> LinkPageByTokenAsync(LinkPageByTokenRequest request)
        {
            try
            {
                // Verify user exists
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }

                // Get Facebook provider
                if (!_providers.TryGetValue("facebook", out var facebookProvider))
                {
                    throw new ArgumentException("Facebook provider is not available");
                }

                // Use Facebook provider to get page info from page access token
                var pageInfo = await facebookProvider.GetPageInfoFromTokenAsync(request.PageAccessToken);
                
                // Try to get user info if user access token is provided
                string userFacebookId = "unknown";
                if (!string.IsNullOrEmpty(request.UserAccessToken))
                {
                    try
                    {
                        var userInfo = await facebookProvider.GetUserInfoFromTokenAsync(request.UserAccessToken);
                        userFacebookId = userInfo.Id;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Could not get user info from user token: {Error}", ex.Message);
                        // Continue with unknown user ID
                    }
                }

                // Check if social account exists for this user and provider
                var existingSocialAccount = await _socialAccountRepository.GetByUserIdAndProviderAsync(request.UserId, "facebook");
                
                SocialAccount socialAccount;
                if (existingSocialAccount != null)
                {
                    // Update existing social account
                    socialAccount = existingSocialAccount;
                    if (!string.IsNullOrEmpty(request.UserAccessToken))
                    {
                        socialAccount.AccessToken = request.UserAccessToken; // Store user token
                        socialAccount.UpdatedAt = DateTime.UtcNow;
                        await _socialAccountRepository.UpdateAsync(socialAccount);
                    }
                }
                else
                {
                    // Create new social account
                    socialAccount = new SocialAccount
                    {
                        UserId = request.UserId,
                        Provider = "facebook",
                        ProviderUserId = userFacebookId,
                        AccessToken = request.UserAccessToken ?? "manual_link", 
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _socialAccountRepository.CreateAsync(socialAccount);
                }

                // Check if this page target already exists
                var existingTarget = await _socialTargetRepository.GetByProviderTargetIdAsync(pageInfo.Id);
                if (existingTarget != null && existingTarget.SocialAccountId == socialAccount.Id)
                {
                    throw new InvalidOperationException("This Facebook page is already linked to your account");
                }

                // Create new social target (Facebook Page)
                var socialTarget = new SocialTarget
                {
                    SocialAccountId = socialAccount.Id,
                    ProviderTargetId = pageInfo.Id,
                    Name = pageInfo.Name,
                    Type = "page",
                    AccessToken = request.PageAccessToken, // Store page access token
                    Category = pageInfo.Category,
                    ProfilePictureUrl = pageInfo.Picture?.Data?.Url,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _socialTargetRepository.CreateAsync(socialTarget);

                // Reload social account with targets
                socialAccount = await _socialAccountRepository.GetByIdWithTargetsAsync(socialAccount.Id);
                
                _logger.LogInformation("Successfully linked Facebook page {PageName} (ID: {PageId}) to user {UserId}", 
                    pageInfo.Name, pageInfo.Id, request.UserId);

                return MapToDto(socialAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking Facebook page by token for user {UserId}", request.UserId);
                throw;
            }
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