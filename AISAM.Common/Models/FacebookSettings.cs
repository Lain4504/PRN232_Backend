namespace AISAM.Common.Models
{
    public class FacebookSettings
    {
        public string AppId { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string GraphApiVersion { get; set; } = "v23.0";
        public string BaseUrl { get; set; } = "https://graph.facebook.com";
        public string OAuthUrl { get; set; } = "https://www.facebook.com";
        public List<string> RequiredPermissions { get; set; } = new()
        {
            "pages_manage_posts",
            "pages_read_engagement", 
            "pages_show_list"
        };
    }
}