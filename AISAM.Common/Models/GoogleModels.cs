namespace AISAM.Common.Models
{
    // Google OAuth Token Response
    public class GoogleTokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string? refresh_token { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; } = string.Empty;
        public string token_type { get; set; } = string.Empty;
        public string? id_token { get; set; }
    }

    // Google User Info
    public class GoogleUserInfo
    {
        public string id { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public bool verified_email { get; set; }
        public string name { get; set; } = string.Empty;
        public string? given_name { get; set; }
        public string? family_name { get; set; }
        public string? picture { get; set; }
        public string? locale { get; set; }
    }

    // Google YouTube Channel Response
    public class GoogleYouTubeChannelResponse
    {
        public string kind { get; set; } = string.Empty;
        public string etag { get; set; } = string.Empty;
        public PageInfo? pageInfo { get; set; }
        public List<YouTubeChannel> items { get; set; } = new();
    }

    public class PageInfo
    {
        public int totalResults { get; set; }
        public int resultsPerPage { get; set; }
    }

    public class YouTubeChannel
    {
        public string kind { get; set; } = string.Empty;
        public string etag { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public ChannelSnippet? snippet { get; set; }
        public ChannelStatistics? statistics { get; set; }
    }

    public class ChannelSnippet
    {
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string? customUrl { get; set; }
        public DateTime publishedAt { get; set; }
        public ChannelThumbnails? thumbnails { get; set; }
    }

    public class ChannelThumbnails
    {
        public ThumbnailItem? @default { get; set; }
        public ThumbnailItem? medium { get; set; }
        public ThumbnailItem? high { get; set; }
    }

    public class ThumbnailItem
    {
        public string url { get; set; } = string.Empty;
        public int width { get; set; }
        public int height { get; set; }
    }

    public class ChannelStatistics
    {
        public string viewCount { get; set; } = string.Empty;
        public string subscriberCount { get; set; } = string.Empty;
        public bool hiddenSubscriberCount { get; set; }
        public string videoCount { get; set; } = string.Empty;
    }

    // Google Error Response
    public class GoogleErrorResponse
    {
        public GoogleError? error { get; set; }
    }

    public class GoogleError
    {
        public int code { get; set; }
        public string message { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public List<GoogleErrorDetail>? errors { get; set; }
    }

    public class GoogleErrorDetail
    {
        public string message { get; set; } = string.Empty;
        public string domain { get; set; } = string.Empty;
        public string reason { get; set; } = string.Empty;
    }
}
