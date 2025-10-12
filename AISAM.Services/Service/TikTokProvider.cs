using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Linq;
using AISAM.Data.Model;

namespace AISAM.Services.Service
{
    public class TikTokProvider : IProviderService
    {
        private readonly HttpClient _httpClient;
        private readonly TikTokSettings _settings;
        private readonly ILogger<TikTokProvider> _logger;

        public string ProviderName => SocialPlatformEnum.TikTok.ToString().ToLower();

        public TikTokProvider(HttpClient httpClient, IOptions<TikTokSettings> settings, ILogger<TikTokProvider> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<string> GetAuthUrlAsync(string state, string redirectUri)
        {
            var scopes = string.Join(",", _settings.RequiredScopes.Distinct());
            var authUrl = $"{_settings.AuthUrl}?" +
                         $"client_key={_settings.ClientKey}" +
                         $"&scope={Uri.EscapeDataString(scopes)}" +
                         $"&response_type=code" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                         $"&state={Uri.EscapeDataString(state)}";

            return Task.FromResult(authUrl);
        }

        public async Task<SocialAccountDto> ExchangeCodeAsync(string code, string redirectUri)
        {
            try
            {
                // Exchange code for access token
                var tokenRequest = new Dictionary<string, string>
                {
                    ["client_key"] = _settings.ClientKey,
                    ["client_secret"] = _settings.ClientSecret,
                    ["code"] = code,
                    ["grant_type"] = "authorization_code",
                    ["redirect_uri"] = redirectUri
                };

                var content = new FormUrlEncodedContent(tokenRequest);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                _logger.LogInformation("Exchanging code for token with TikTok");

                var tokenResponse = await _httpClient.PostAsync(_settings.TokenUrl, content);
                
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogError("TikTok token exchange failed with status {StatusCode}: {ErrorContent}", 
                        tokenResponse.StatusCode, errorContent);
                    
                    // Try to parse TikTok error response
                    try
                    {
                        var tiktokError = JsonSerializer.Deserialize<TikTokErrorResponse>(errorContent);
                        if (tiktokError?.Error != null)
                        {
                            var errorMessage = GetTikTokErrorMessage(tiktokError.Error);
                            throw new InvalidOperationException(errorMessage);
                        }
                    }
                    catch (JsonException)
                    {
                        // If we can't parse as TikTok error, use generic error
                    }
                    
                    throw new HttpRequestException($"TikTok token exchange failed: {tokenResponse.StatusCode} - {errorContent}");
                }

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("TikTok token response received");
                
                var tokenData = JsonSerializer.Deserialize<TikTokTokenResponse>(tokenJson);

                if (tokenData?.AccessToken == null)
                {
                    _logger.LogError("Failed to parse access token from response: {TokenJson}", tokenJson);
                    throw new InvalidOperationException($"Không thể lấy access token từ TikTok. Phản hồi: {tokenJson}");
                }

                // Get user info using the open_id from token response or make API call
                string openId;
                if (!string.IsNullOrEmpty(tokenData.OpenId))
                {
                    openId = tokenData.OpenId;
                }
                else
                {
                    // Fallback: call user info API to get open_id
                    var userInfo = await GetUserInfoAsync(tokenData.AccessToken);
                    openId = userInfo.OpenId;
                }

                return new SocialAccountDto
                {
                    Provider = ProviderName,
                    ProviderUserId = openId,
                    AccessToken = tokenData.AccessToken,
                    RefreshToken = tokenData.RefreshToken,
                    IsActive = true,
                    ExpiresAt = tokenData.ExpiresIn.HasValue ? DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn.Value) : null,
                    CreatedAt = DateTime.UtcNow,
                    Targets = new List<SocialTargetDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging TikTok code for token");
                throw;
            }
        }

        public async Task<IEnumerable<AvailableTargetDto>> GetTargetsAsync(string accessToken)
        {
            // TikTok doesn't have pages/business accounts like Facebook yet
            // Return empty list for now - will be extended in future
            await Task.CompletedTask;
            return new List<AvailableTargetDto>();
        }

        public async Task<Dictionary<string, string>> GetTargetAccessTokensAsync(string userAccessToken, IEnumerable<string> providerTargetIds)
        {
            // TikTok doesn't have page tokens like Facebook yet
            // Return empty dictionary for now
            await Task.CompletedTask;
            return new Dictionary<string, string>();
        }

        public async Task<PublishResultDto> PublishAsync(SocialAccount account, SocialIntegration integration, PostDto post)
        {
            // TikTok posting implementation will be added in future
            await Task.CompletedTask;
            return new PublishResultDto
            {
                Success = false,
                ErrorMessage = "TikTok posting chưa được hỗ trợ"
            };
        }

        public async Task RefreshTokenIfNeededAsync(SocialAccount account)
        {
            if (string.IsNullOrEmpty(account.RefreshToken) || !account.ExpiresAt.HasValue)
            {
                return;
            }

            // Check if token expires within 7 days
            if (account.ExpiresAt.Value > DateTime.UtcNow.AddDays(7))
            {
                return;
            }

            try
            {
                _logger.LogInformation("Refreshing TikTok token for account {AccountId}", account.Id);

                var refreshRequest = new Dictionary<string, string>
                {
                    ["client_key"] = _settings.ClientKey,
                    ["client_secret"] = _settings.ClientSecret,
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = account.RefreshToken
                };

                var content = new FormUrlEncodedContent(refreshRequest);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                var response = await _httpClient.PostAsync(_settings.TokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("TikTok token refresh failed: {StatusCode} - {ErrorContent}", 
                        response.StatusCode, errorContent);
                    return;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<TikTokTokenResponse>(responseJson);

                if (tokenData?.AccessToken != null)
                {
                    account.UserAccessToken = tokenData.AccessToken;
                    account.RefreshToken = tokenData.RefreshToken ?? account.RefreshToken;
                    account.ExpiresAt = tokenData.ExpiresIn.HasValue ? DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn.Value) : account.ExpiresAt;
                    account.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Successfully refreshed TikTok token for account {AccountId}", account.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing TikTok token for account {AccountId}", account.Id);
            }
        }

        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            try
            {
                var userInfo = await GetUserInfoAsync(accessToken);
                return !string.IsNullOrEmpty(userInfo.OpenId);
            }
            catch
            {
                return false;
            }
        }

        private async Task<TikTokUserInfo> GetUserInfoAsync(string accessToken)
        {
            var userInfoUrl = $"{_settings.UserInfoUrl}?fields=open_id";
            
            var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var userInfoResponse = JsonSerializer.Deserialize<TikTokUserInfoResponse>(json);

            if (userInfoResponse?.Data.User == null)
            {
                throw new InvalidOperationException("Không thể lấy thông tin người dùng từ TikTok");
            }

            return userInfoResponse.Data.User;
        }

        private static string GetTikTokErrorMessage(TikTokError error)
        {
            return error.Code switch
            {
                "invalid_code" => "Mã xác thực không hợp lệ hoặc đã hết hạn",
                "invalid_client" => "Thông tin ứng dụng TikTok không hợp lệ",
                "invalid_grant" => "Quyền truy cập không hợp lệ",
                "invalid_redirect_uri" => "URL chuyển hướng không hợp lệ",
                "invalid_scope" => "Quyền truy cập không được hỗ trợ",
                "server_error" => "Lỗi máy chủ TikTok. Vui lòng thử lại sau",
                "temporarily_unavailable" => "Dịch vụ TikTok tạm thời không khả dụng",
                _ => $"Lỗi TikTok: {error.Message}"
            };
        }
    }
}
