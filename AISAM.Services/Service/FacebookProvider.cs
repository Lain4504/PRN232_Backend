using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Linq;

namespace AISAM.Services.Service
{
    public class FacebookProvider : IProviderService
    {
        private readonly HttpClient _httpClient;
        private readonly FacebookSettings _settings;
        private readonly ILogger<FacebookProvider> _logger;

        public string ProviderName => "facebook";

        public FacebookProvider(HttpClient httpClient, IOptions<FacebookSettings> settings, ILogger<FacebookProvider> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<string> GetAuthUrlAsync(string state, string redirectUri)
        {
            var permissions = string.Join(",", _settings.RequiredPermissions?.Distinct() ?? Enumerable.Empty<string>());
            var authUrl = $"{_settings.OAuthUrl}/v20.0/dialog/oauth?" +
                         $"client_id={_settings.AppId}" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                         $"&scope={Uri.EscapeDataString(permissions)}" +
                         $"&response_type=code" +
                         $"&state={Uri.EscapeDataString(state)}";

            return Task.FromResult(authUrl);
        }

        public async Task<SocialAccountDto> ExchangeCodeAsync(string code, string redirectUri)
        {
            try
            {
                // Exchange code for access token
                var tokenUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/oauth/access_token?" +
                              $"client_id={_settings.AppId}" +
                              $"&client_secret={_settings.AppSecret}" +
                              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                              $"&code={Uri.EscapeDataString(code)}";

                _logger.LogInformation("Exchanging code for token with URL: {TokenUrl}", tokenUrl.Replace(_settings.AppSecret, "***SECRET***"));

                var tokenResponse = await _httpClient.GetAsync(tokenUrl);
                
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Facebook token exchange failed with status {StatusCode}: {ErrorContent}", 
                        tokenResponse.StatusCode, errorContent);
                    throw new HttpRequestException($"Facebook token exchange failed: {tokenResponse.StatusCode} - {errorContent}");
                }

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Facebook token response: {TokenResponse}", tokenJson);
                
                var tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(tokenJson);

                if (tokenData?.AccessToken == null)
                {
                    _logger.LogError("Failed to parse access token from response: {TokenJson}", tokenJson);
                    throw new InvalidOperationException($"Failed to obtain access token from Facebook. Response: {tokenJson}");
                }

                // Get user info
                var userUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/me?fields=id,name,email&access_token={tokenData.AccessToken}";
                var userResponse = await _httpClient.GetAsync(userUrl);
                userResponse.EnsureSuccessStatusCode();

                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userData = JsonSerializer.Deserialize<FacebookUserResponse>(userJson);

                if (userData?.Id == null)
                {
                    throw new InvalidOperationException("Failed to get user info from Facebook");
                }

                return new SocialAccountDto
                {
                    Provider = ProviderName,
                    ProviderUserId = userData.Id,
                    AccessToken = tokenData.AccessToken,
                    IsActive = true,
                    ExpiresAt = tokenData.ExpiresIn.HasValue ? DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn.Value) : null,
                    CreatedAt = DateTime.UtcNow,
                    Targets = new List<SocialTargetDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging Facebook code for token");
                throw;
            }
        }

        public async Task<IEnumerable<SocialTargetDto>> GetTargetsAsync(string accessToken)
        {
            try
            {
                var pagesUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/me/accounts?" +
                              $"fields=id,name,category,picture,access_token&access_token={accessToken}";

                var response = await _httpClient.GetAsync(pagesUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var pagesData = JsonSerializer.Deserialize<FacebookPageResponse>(json);

                if (pagesData?.Data == null)
                {
                    return new List<SocialTargetDto>();
                }

                return pagesData.Data.Select(page => new SocialTargetDto
                {
                    ProviderTargetId = page.Id,
                    Name = page.Name ?? "",
                    Type = "page",
                    Category = page.Category,
                    ProfilePictureUrl = page.Picture?.Data?.Url,
                    IsActive = true
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Facebook pages");
                throw;
            }
        }

        public async Task<PublishResultDto> PublishAsync(SocialAccount account, SocialTarget target, PostDto post)
        {
            try
            {
                var pageAccessToken = target.AccessToken ?? account.AccessToken;
                var publishUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/{target.ProviderTargetId}/feed";

                var postData = new Dictionary<string, string>
                {
                    ["message"] = post.Message,
                    ["access_token"] = pageAccessToken
                };

                if (!string.IsNullOrEmpty(post.LinkUrl))
                {
                    postData["link"] = post.LinkUrl;
                }

                if (!string.IsNullOrEmpty(post.ImageUrl))
                {
                    postData["picture"] = post.ImageUrl;
                }

                var formContent = new FormUrlEncodedContent(postData);
                var response = await _httpClient.PostAsync(publishUrl, formContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<FacebookPostResponse>(responseJson);

                    return new PublishResultDto
                    {
                        Success = true,
                        ProviderPostId = responseData?.Id,
                        PostedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Facebook API error: {Error}", errorJson);

                    return new PublishResultDto
                    {
                        Success = false,
                        ErrorMessage = $"Facebook API error: {response.StatusCode} - {errorJson}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing to Facebook");
                return new PublishResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task RefreshTokenIfNeededAsync(SocialAccount account)
        {
            // Facebook short-lived tokens can be exchanged for long-lived tokens
            // Implementation depends on token type and requirements
            if (account.ExpiresAt.HasValue && account.ExpiresAt.Value <= DateTime.UtcNow.AddDays(7))
            {
                _logger.LogWarning("Facebook token for account {AccountId} is expiring soon", account.Id);
                // TODO: Implement token refresh logic
            }
            
            return Task.CompletedTask;
        }

        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            try
            {
                var validateUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/me?access_token={accessToken}";
                var response = await _httpClient.GetAsync(validateUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<FacebookPageInfo> GetPageInfoFromTokenAsync(string pageAccessToken)
        {
            try
            {
                var pageUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/me?fields=id,name,category,picture&access_token={pageAccessToken}";
                var response = await _httpClient.GetAsync(pageUrl);
                response.EnsureSuccessStatusCode();

                var pageJson = await response.Content.ReadAsStringAsync();
                var pageData = JsonSerializer.Deserialize<FacebookPageData>(pageJson);

                if (pageData?.Id == null)
                {
                    throw new InvalidOperationException("Failed to get page info from Facebook");
                }

                return new FacebookPageInfo
                {
                    Id = pageData.Id,
                    Name = pageData.Name,
                    Category = pageData.Category,
                    Picture = pageData.Picture
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Facebook page info from token");
                throw;
            }
        }

        public async Task<FacebookUserInfo> GetUserInfoFromTokenAsync(string userAccessToken)
        {
            try
            {
                var userUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/me?fields=id,name,email&access_token={userAccessToken}";
                var response = await _httpClient.GetAsync(userUrl);
                response.EnsureSuccessStatusCode();

                var userJson = await response.Content.ReadAsStringAsync();
                var userData = JsonSerializer.Deserialize<FacebookUserResponse>(userJson);

                if (userData?.Id == null)
                {
                    throw new InvalidOperationException("Failed to get user info from Facebook");
                }

                return new FacebookUserInfo
                {
                    Id = userData.Id,
                    Name = userData.Name,
                    Email = userData.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Facebook user info from token");
                throw;
            }
        }
    }
}