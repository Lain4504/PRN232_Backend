using System.Text.Json.Serialization;

namespace AISAM.Common.Models
{
    public class FacebookAdAccountsResponse
    {
        [JsonPropertyName("data")]
        public List<FacebookAdAccount> Data { get; set; } = new();
    }

    public class FacebookAdAccount
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("account_id")]
        public string AccountId { get; set; } = string.Empty;
        
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
        
        [JsonPropertyName("timezone_name")]
        public string TimezoneName { get; set; } = string.Empty;
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}