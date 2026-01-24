using AISAM.Common.Config;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AISAM.Services.Service
{
    public class GoogleProvider : IProviderService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleSettings _settings;
        private readonly ILogger<GoogleProvider> _logger;

        private const string GoogleOAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string GoogleTokenUrl = "https://oauth2.googleapis.com/token";
        private const string GoogleUserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";
        private const string YouTubeApiUrl = "https://www.googleapis.com/youtube/v3";

        public string ProviderName => SocialPlatformEnum.Google.ToString().ToLower();

        public GoogleProvider(
            HttpClient httpClient,
            IOptions<GoogleSettings> settings,
            ILogger<GoogleProvider> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<string> GetAuthUrlAsync(string state, string redirectUri)
        {
            var scopes = string.Join(" ", _settings.RequiredScopes);
            var authUrl = $"{GoogleOAuthUrl}?" +
                         $"client_id={_settings.ClientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                         $"&response_type=code" +
                         $"&scope={Uri.EscapeDataString(scopes)}" +
                         $"&access_type=offline" + // Get refresh token
                         $"&prompt=consent" + // Force consent screen to get refresh token
                         $"&state={Uri.EscapeDataString(state)}";

            _logger.LogInformation("Generated Google OAuth URL for state: {State}", state);
            return Task.FromResult(authUrl);
        }

        public async Task<SocialAccountDto> ExchangeCodeAsync(string code, string redirectUri)
        {
            try
            {
                _logger.LogInformation("Exchanging Google authorization code for access token");

                // Exchange code for tokens
                var tokenRequest = new Dictionary<string, string>
                {
                    { "code", code },
                    { "client_id", _settings.ClientId },
                    { "client_secret", _settings.ClientSecret },
                    { "redirect_uri", redirectUri },
                    { "grant_type", "authorization_code" }
                };

                var tokenResponse = await _httpClient.PostAsync(
                    GoogleTokenUrl,
                    new FormUrlEncodedContent(tokenRequest));

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Google token exchange failed: {Error}", errorContent);
                    throw new InvalidOperationException($"Failed to exchange code: {errorContent}");
                }

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(tokenJson);

                if (tokenData?.access_token == null)
                {
                    throw new InvalidOperationException("Failed to obtain access token from Google");
                }

                // Get user info
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenData.access_token}");

                var userResponse = await _httpClient.GetAsync(GoogleUserInfoUrl);
                if (!userResponse.IsSuccessStatusCode)
                {
                    var errorContent = await userResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get Google user info: {Error}", errorContent);
                    throw new InvalidOperationException("Failed to get user information from Google");
                }

                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userData = JsonSerializer.Deserialize<GoogleUserInfo>(userJson);

                if (userData?.id == null)
                {
                    throw new InvalidOperationException("Failed to parse Google user data");
                }

                _logger.LogInformation("Successfully authenticated Google user: {UserId}", userData.id);

                return new SocialAccountDto
                {
                    ProviderUserId = userData.id,
                    AccessToken = tokenData.access_token,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in),
                    IsActive = true,
                    Provider = ProviderName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during Google OAuth");
                throw new InvalidOperationException("Failed to connect to Google. Please try again.", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Google API response");
                throw new InvalidOperationException("Failed to parse Google response", ex);
            }
        }

        public async Task<IEnumerable<AvailableTargetDto>> GetTargetsAsync(string accessToken)
        {
            try
            {
                _logger.LogInformation("Fetching YouTube channels for user");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                // Get user's YouTube channels
                var channelsUrl = $"{YouTubeApiUrl}/channels?part=snippet,statistics&mine=true";
                var response = await _httpClient.GetAsync(channelsUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to fetch YouTube channels: {Error}", errorContent);
                    
                    // Check if user doesn't have a YouTube channel
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _logger.LogWarning("User doesn't have access to YouTube API or no YouTube channel");
                        return new List<AvailableTargetDto>();
                    }

                    throw new InvalidOperationException($"Failed to fetch YouTube channels: {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var channelsData = JsonSerializer.Deserialize<GoogleYouTubeChannelResponse>(json);

                if (channelsData?.items == null || !channelsData.items.Any())
                {
                    _logger.LogInformation("User has no YouTube channels");
                    return new List<AvailableTargetDto>();
                }

                var targets = channelsData.items.Select(channel => new AvailableTargetDto
                {
                    ProviderTargetId = channel.id,
                    Name = channel.snippet?.title ?? "Unknown Channel",
                    Type = "youtube_channel",
                    ProfilePictureUrl = channel.snippet?.thumbnails?.high?.url 
                                     ?? channel.snippet?.thumbnails?.medium?.url
                                     ?? channel.snippet?.thumbnails?.@default?.url,
                    IsActive = true
                }).ToList();

                _logger.LogInformation("Found {Count} YouTube channels", targets.Count);
                return targets;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching YouTube channels");
                throw new InvalidOperationException("Failed to connect to YouTube API", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse YouTube API response");
                throw new InvalidOperationException("Failed to parse YouTube response", ex);
            }
        }

        public Task<Dictionary<string, string>> GetTargetAccessTokensAsync(
            string userAccessToken,
            IEnumerable<string> providerTargetIds)
        {
            // For YouTube, we use the same access token for all channels
            // The user's access token has access to all their channels
            var result = new Dictionary<string, string>();
            foreach (var targetId in providerTargetIds)
            {
                result[targetId] = userAccessToken;
            }
            return Task.FromResult(result);
        }

        public async Task<PublishResultDto> PublishAsync(
            SocialAccount account,
            SocialIntegration integration,
            PostDto post)
        {
            try
            {
                _logger.LogInformation("Publishing video to YouTube channel: {ChannelId}", integration.ExternalId);

                // Ensure token is valid
                await RefreshTokenIfNeededAsync(account);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {account.UserAccessToken}");

                // YouTube video upload requires multipart request
                // This is a simplified version - actual implementation would need to:
                // 1. Upload video file to YouTube
                // 2. Set video metadata
                // 3. Return video ID

                // For now, we'll just validate the post data
                if (string.IsNullOrEmpty(post.Message))
                {
                    throw new ArgumentException("Post message (video title) is required");
                }

                // TODO: Implement actual YouTube video upload
                // This requires:
                // - Uploading video file using resumable upload
                // - Setting video metadata (title, description, tags, privacy)
                // - Handling thumbnails
                
                _logger.LogWarning("YouTube video publishing is not fully implemented yet");
                
                throw new NotImplementedException(
                    "YouTube video publishing requires video file upload implementation. " +
                    "Please implement resumable upload for video files.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing to YouTube");
                throw;
            }
        }

        public async Task RefreshTokenIfNeededAsync(SocialAccount account)
        {
            // Check if token is about to expire (within 5 minutes)
            if (account.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                return; // Token is still valid
            }

            if (string.IsNullOrEmpty(account.RefreshToken))
            {
                throw new InvalidOperationException(
                    "No refresh token available. User needs to re-authenticate.");
            }

            try
            {
                _logger.LogInformation("Refreshing Google access token for account: {AccountId}", account.AccountId);

                var refreshRequest = new Dictionary<string, string>
                {
                    { "client_id", _settings.ClientId },
                    { "client_secret", _settings.ClientSecret },
                    { "refresh_token", account.RefreshToken },
                    { "grant_type", "refresh_token" }
                };

                var response = await _httpClient.PostAsync(
                    GoogleTokenUrl,
                    new FormUrlEncodedContent(refreshRequest));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to refresh Google token: {Error}", errorContent);
                    throw new InvalidOperationException("Failed to refresh access token");
                }

                var json = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(json);

                if (tokenData?.access_token == null)
                {
                    throw new InvalidOperationException("Failed to parse refresh token response");
                }

                // Update account with new token
                account.UserAccessToken = tokenData.access_token;
                account.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in);
                account.UpdatedAt = DateTime.UtcNow;

                // Note: Google doesn't always return a new refresh token
                if (!string.IsNullOrEmpty(tokenData.refresh_token))
                {
                    account.RefreshToken = tokenData.refresh_token;
                }

                _logger.LogInformation("Successfully refreshed Google access token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Google token");
                throw new InvalidOperationException("Failed to refresh Google access token", ex);
            }
        }

        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(GoogleUserInfoUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google token");
                return false;
            }
        }
    }
}
