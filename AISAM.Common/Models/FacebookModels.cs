using System.Text.Json.Serialization;

namespace AISAM.Common.Models
{
    public class FacebookTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }
    
    public class FacebookUserResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
    
    public class FacebookPageResponse
    {
        [JsonPropertyName("data")]
        public List<FacebookPageData> Data { get; set; } = new();
    }
    
    public class FacebookPageData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("category")]
        public string? Category { get; set; }
        
        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; set; }
    }
    
    public class FacebookPicture
    {
        [JsonPropertyName("data")]
        public FacebookPictureData Data { get; set; } = new();
    }
    
    public class FacebookPictureData
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
    
    public class FacebookPostResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("post_id")]
        public string? PostId { get; set; }
    }
    
    public class FacebookErrorResponse
    {
        [JsonPropertyName("error")]
        public FacebookError Error { get; set; } = new();
    }
    
    public class FacebookError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [JsonPropertyName("fbtrace_id")]
        public string? FbTraceId { get; set; }
    }
    
    // Additional models for token-based operations
    public class FacebookPageInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public FacebookPicture? Picture { get; set; }
    }
    
    public class FacebookUserInfo  
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}