namespace AISAM.Common.Dtos.Response
{
    public class FacebookPostDetails
    {
        public string Id { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Title { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? LinkUrl { get; set; }
        public DateTime CreatedTime { get; set; }
        public string? Type { get; set; }
    }
}
