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
        public bool UseSandbox { get; set; } = false; // Default to production
        public SandboxConfig? Sandbox { get; set; }
        public List<string> RequiredPermissions { get; set; } = new()
        {
            "pages_manage_posts",
            "pages_read_engagement", 
            "pages_show_list"
        };
    }

    public class SandboxConfig 
    {
        public string AppId { get; set; } = "1128719149388470";
        public string AccessToken { get; set; } = string.Empty;
        public string AdAccountId { get; set; } = "4005413613009650";
        public string PageId { get; set; } = "61582203080746";
        public string UserId { get; set; } = "122099098875073436";
        public string Currency { get; set; } = "VND"; // Vietnamese Dong
        public string Timezone { get; set; } = "America/Los_Angeles"; // Ad account timezone
    }
}