using System.Text.Json.Serialization;

namespace AISAM.Common.Models
{
    public class TikTokTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
        
        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
        
        [JsonPropertyName("refresh_expires_in")]
        public int? RefreshExpiresIn { get; set; }
        
        [JsonPropertyName("open_id")]
        public string OpenId { get; set; } = string.Empty;
        
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
    
    public class TikTokUserInfoResponse
    {
        [JsonPropertyName("data")]
        public TikTokUserInfoData Data { get; set; } = new();
    }
    
    public class TikTokUserInfoData
    {
        [JsonPropertyName("user")]
        public TikTokUserInfo User { get; set; } = new();
    }
    
    public class TikTokUserInfo
    {
        [JsonPropertyName("open_id")]
        public string OpenId { get; set; } = string.Empty;
        
        [JsonPropertyName("union_id")]
        public string? UnionId { get; set; }
        
        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
        
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
    
    public class TikTokErrorResponse
    {
        [JsonPropertyName("error")]
        public TikTokError Error { get; set; } = new();
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        
        [JsonPropertyName("log_id")]
        public string? LogId { get; set; }
    }
    
    public class TikTokError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonPropertyName("log_id")]
        public string? LogId { get; set; }
    }
}
