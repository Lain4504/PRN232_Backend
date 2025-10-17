using System.Text.Json.Serialization;

namespace AISAM.Common.Dtos.Request
{
    public class SupabaseBeforeUserCreatedEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("user")]
        public SupabaseUserPayload? User { get; set; }
    }

    public class SupabaseUserPayload
    {
        // Supabase user id (UUID string)
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}


