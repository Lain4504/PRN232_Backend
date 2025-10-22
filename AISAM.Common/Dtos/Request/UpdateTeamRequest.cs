using AISAM.Data.Enumeration;
using System.Text.Json.Serialization;

namespace AISAM.Common.Dtos.Request
{
    public class UpdateTeamRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TeamStatusEnum? Status { get; set; }
    }
}
