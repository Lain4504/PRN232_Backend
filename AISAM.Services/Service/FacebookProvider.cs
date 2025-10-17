using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Linq;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;

namespace AISAM.Services.Service
{
    public class FacebookProvider : IProviderService
    {
        private readonly HttpClient _httpClient;
        private readonly FacebookSettings _settings;
        private readonly ILogger<FacebookProvider> _logger;

        public string ProviderName => SocialPlatformEnum.Facebook.ToString().ToLower();

        public FacebookProvider(HttpClient httpClient, IOptions<FacebookSettings> settings, ILogger<FacebookProvider> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<string> GetAuthUrlAsync(string state, string redirectUri)
        {
            var permissions = string.Join(",", _settings.RequiredPermissions?.Distinct() ?? Enumerable.Empty<string>());
            var authUrl = $"{_settings.OAuthUrl}/{_settings.GraphApiVersion}/dialog/oauth?" +
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
                    
                    // Try to parse Facebook error response
                    try
                    {
                        var facebookError = JsonSerializer.Deserialize<FacebookErrorResponse>(errorContent);
                        if (facebookError?.Error != null)
                        {
                            var error = facebookError.Error;
                            var errorMessage = GetFacebookErrorMessage(error);
                            throw new InvalidOperationException(errorMessage);
                        }
                    }
                    catch (JsonException)
                    {
                        // If we can't parse as Facebook error, use generic error
                    }
                    
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

        public async Task<IEnumerable<AvailableTargetDto>> GetTargetsAsync(string accessToken)
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
                    return new List<AvailableTargetDto>();
                }

                return pagesData.Data.Select(page => new AvailableTargetDto
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

        public async Task<Dictionary<string, string>> GetTargetAccessTokensAsync(string userAccessToken, IEnumerable<string> providerTargetIds)
        {
            try
            {
                var pagesUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/me/accounts?fields=id,access_token&access_token={userAccessToken}";
                var response = await _httpClient.GetAsync(pagesUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var pagesData = JsonSerializer.Deserialize<FacebookPageResponse>(json);
                var idSet = new HashSet<string>(providerTargetIds);
                var result = new Dictionary<string, string>();

                foreach (var page in pagesData?.Data ?? new List<FacebookPageData>())
                {
                    if (!string.IsNullOrEmpty(page.Id) && idSet.Contains(page.Id) && !string.IsNullOrEmpty(page.AccessToken))
                    {
                        result[page.Id] = page.AccessToken;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Facebook page access tokens");
                throw;
            }
        }

        public async Task<PublishResultDto> PublishAsync(SocialAccount account, SocialIntegration integration, PostDto post)
        {
            try
            {
                var publishUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/{integration.ExternalId}/feed";

                async Task<(bool ok, string body)> TryPublishAsync(string accessToken)
                {
                    var postData = new Dictionary<string, string>
                    {
                        ["message"] = post.Message,
                        ["access_token"] = accessToken
                    };

                    if (!string.IsNullOrEmpty(post.LinkUrl))
                    {
                        postData["link"] = post.LinkUrl;
                    }

                    // For single image, Facebook allows 'picture' on feed with link, but for multiple images we need attached_media

                    var formContent = new FormUrlEncodedContent(postData);
                    var response = await _httpClient.PostAsync(publishUrl, formContent);
                    var body = await response.Content.ReadAsStringAsync();
                    return (response.IsSuccessStatusCode, body);
                }

                // If multiple images, upload each as unpublished photo and attach
                if (post.ImageUrls != null && post.ImageUrls.Count > 1)
                {
                    var accessTokenForMedia = integration.AccessToken ?? account.UserAccessToken;
                    var uploadedMediaIds = new List<string>();
                    foreach (var imageUrl in post.ImageUrls)
                    {
                        var mediaUploadUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/{integration.ExternalId}/photos";
                        var mediaData = new Dictionary<string, string>
                        {
                            ["url"] = imageUrl,
                            ["published"] = "false",
                            ["access_token"] = accessTokenForMedia
                        };
                        var mediaResp = await _httpClient.PostAsync(mediaUploadUrl, new FormUrlEncodedContent(mediaData));
                        var mediaBody = await mediaResp.Content.ReadAsStringAsync();
                        if (!mediaResp.IsSuccessStatusCode)
                        {
                            _logger.LogError("Facebook media upload failed: {Error}", mediaBody);
                            return new PublishResultDto { Success = false, ErrorMessage = $"Facebook media upload error: {mediaBody}" };
                        }
                        var mediaObj = JsonSerializer.Deserialize<FacebookPostResponse>(mediaBody);
                        if (!string.IsNullOrEmpty(mediaObj?.Id))
                        {
                            uploadedMediaIds.Add(mediaObj.Id);
                        }
                    }

                    // Now publish feed with attached_media
                    var feedUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/{integration.ExternalId}/feed";
                    var postFields = new List<KeyValuePair<string, string>>
                    {
                        new("message", post.Message),
                        new("access_token", accessTokenForMedia)
                    };
                    for (int i = 0; i < uploadedMediaIds.Count; i++)
                    {
                        var key = $"attached_media[{i}]";
                        var value = JsonSerializer.Serialize(new { media_fbid = uploadedMediaIds[i] });
                        postFields.Add(new KeyValuePair<string, string>(key, value));
                    }
                    var feedResp = await _httpClient.PostAsync(feedUrl, new FormUrlEncodedContent(postFields));
                    var feedBody = await feedResp.Content.ReadAsStringAsync();
                    if (!feedResp.IsSuccessStatusCode)
                    {
                        _logger.LogError("Facebook feed publish with attached_media failed: {Error}", feedBody);
                        return new PublishResultDto { Success = false, ErrorMessage = $"Facebook API error: {feedBody}" };
                    }
                    var feedObj = JsonSerializer.Deserialize<FacebookPostResponse>(feedBody);
                    return new PublishResultDto { Success = true, ProviderPostId = feedObj?.Id, PostedAt = DateTime.UtcNow };
                }

                // If video provided, use video upload endpoint
                if (!string.IsNullOrEmpty(post.VideoUrl))
                {
                    var videosUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/{integration.ExternalId}/videos";
                    var videoData = new Dictionary<string, string>
                    {
                        ["file_url"] = post.VideoUrl,
                        ["description"] = post.Message,
                        ["access_token"] = integration.AccessToken ?? account.UserAccessToken
                    };
                    var videoResp = await _httpClient.PostAsync(videosUrl, new FormUrlEncodedContent(videoData));
                    var videoBody = await videoResp.Content.ReadAsStringAsync();
                    if (!videoResp.IsSuccessStatusCode)
                    {
                        _logger.LogError("Facebook video publish failed: {Error}", videoBody);
                        return new PublishResultDto { Success = false, ErrorMessage = $"Facebook API error: {videoBody}" };
                    }
                    var videoObj = JsonSerializer.Deserialize<FacebookPostResponse>(videoBody);
                    return new PublishResultDto { Success = true, ProviderPostId = videoObj?.Id, PostedAt = DateTime.UtcNow };
                }

                // 1) Try with existing integration token or account token for text/single-image
                var initialToken = integration.AccessToken ?? account.UserAccessToken;
                var (ok, body) = await TryPublishAsync(initialToken);
                if (ok)
                {
                    var responseData = JsonSerializer.Deserialize<FacebookPostResponse>(body);
                    return new PublishResultDto
                    {
                        Success = true,
                        ProviderPostId = responseData?.Id,
                        PostedAt = DateTime.UtcNow
                    };
                }

                // 2) If failed, detect token/permission error and lazy-fetch fresh page token, then retry once
                var needsPageToken = false;
                try
                {
                    var errorWrapper = JsonSerializer.Deserialize<FacebookErrorResponse>(body);
                    var code = errorWrapper?.Error?.Code;
                    var message = errorWrapper?.Error?.Message ?? string.Empty;
                    // code 190 = invalid token, code 200 = permissions; message hints posting to page requires page token and proper scopes
                    needsPageToken = code == 190 || code == 200 ||
                                     message.Contains("requires page token", StringComparison.OrdinalIgnoreCase) ||
                                     message.Contains("pages_manage_posts", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    // ignore parse errors
                }

                if (!needsPageToken)
                {
                    _logger.LogError("Facebook API error: {Error}", body);
                    return new PublishResultDto
                    {
                        Success = false,
                        ErrorMessage = $"Facebook API error: {body}"
                    };
                }

                // Fetch fresh page token from /me/accounts
                var tokenMap = await GetTargetAccessTokensAsync(account.UserAccessToken, new[] { integration.ExternalId ?? "" });
                if (!tokenMap.TryGetValue(integration.ExternalId ?? "", out var freshPageToken))
                {
                    _logger.LogError("Could not refresh page access token for page {PageId}", integration.ExternalId);
                    return new PublishResultDto { Success = false, ErrorMessage = "Unable to refresh page access token" };
                }

                var (ok2, body2) = await TryPublishAsync(freshPageToken);
                if (!ok2)
                {
                    _logger.LogError("Facebook API error after refresh: {Error}", body2);
                    return new PublishResultDto { Success = false, ErrorMessage = $"Facebook API error: {body2}" };
                }

                var responseData2 = JsonSerializer.Deserialize<FacebookPostResponse>(body2);
                return new PublishResultDto { Success = true, ProviderPostId = responseData2?.Id, PostedAt = DateTime.UtcNow, RefreshedTargetAccessToken = freshPageToken };
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
        
        public async Task<IEnumerable<AdAccountDto>> GetAdAccountsAsync(string accessToken)
        {
            try
            {
                var adAccountsUrl = $"{_settings.BaseUrl}/{_settings.GraphApiVersion}/me/adaccounts?" +
                                    $"fields=id,name,account_id,currency,timezone_name&access_token={accessToken}";
        
                _logger.LogInformation("Requesting Facebook ad accounts from: {Url}", adAccountsUrl);
                
                var response = await _httpClient.GetAsync(adAccountsUrl);
                response.EnsureSuccessStatusCode();
        
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Facebook ad accounts response: {Response}", json);
                
                var adAccountsResponse = JsonSerializer.Deserialize<FacebookAdAccountsResponse>(json);
                _logger.LogInformation("Deserialized response: Data count = {Count}", adAccountsResponse?.Data?.Count ?? 0);
        
                if (adAccountsResponse?.Data == null)
                {
                    _logger.LogWarning("AdAccountsResponse.Data is null");
                    return new List<AdAccountDto>();
                }
        
                var result = adAccountsResponse.Data.Select(account => 
                {
                    _logger.LogInformation("Processing account: {Id} - {Name}", account.Id, account.Name);
                    return new AdAccountDto
                    {
                        Id = account.Id,
                        Name = account.Name,
                        AccountId = account.AccountId,
                        Currency = account.Currency,
                        Timezone = account.TimezoneName,
                        Status = "ACTIVE" // Default status since Facebook doesn't provide it
                    };
                }).ToList();
                
                _logger.LogInformation("Final result count: {Count}", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Facebook ad accounts");
                throw;
            }
        }

        private static string GetFacebookErrorMessage(FacebookError error)
        {
            // Handle specific Facebook OAuth error codes
            switch (error.Code)
            {
                case 100 when error.ErrorSubcode == 36009:
                    return "Mã xác thực đã được sử dụng. Vui lòng thử lại quá trình đăng nhập Facebook.";
                case 100:
                    return $"Lỗi xác thực Facebook: {error.Message}";
                case 190:
                    return "Token Facebook đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.";
                case 200:
                    return "Không có quyền truy cập. Vui lòng cấp quyền cần thiết.";
                case 102:
                    return "Phiên đăng nhập Facebook đã hết hạn. Vui lòng đăng nhập lại.";
                default:
                    return $"Lỗi Facebook ({error.Code}): {error.Message}";
            }
        }
    }
}