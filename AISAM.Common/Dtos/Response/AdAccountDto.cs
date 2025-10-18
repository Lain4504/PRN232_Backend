namespace AISAM.Common.Dtos.Response
{
    public class AdAccountDto
    {
        public string Id { get; set; } = string.Empty; // Facebook Ad Account ID
        public string Name { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}