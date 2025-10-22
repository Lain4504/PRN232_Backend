using AISAM.Data.Enumeration;
using System.Text.Json.Serialization;

namespace AISAM.Common.Dtos.Request
{
    public class UpdateContentRequest
    {
        public string? Title { get; set; }
        public string? TextContent { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AdTypeEnum? AdType { get; set; }
        
        public Guid? ProductId { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? StyleDescription { get; set; }
        public string? ContextDescription { get; set; }
        public string? RepresentativeCharacter { get; set; }
    }
}
