namespace AISAM.Common.Models
{
    public class TikTokSettings
    {
        public string ClientKey { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string AuthUrl { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public string UserInfoUrl { get; set; } = string.Empty;
        public List<string> RequiredScopes { get; set; } = new();
    }
}
