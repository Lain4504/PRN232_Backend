namespace AISAM.Common.Dtos.Response
{
    public class SocialIntegrationDto
    {
        public Guid Id { get; set; }
        public Guid SocialAccountId { get; set; }
        public Guid ProfileId { get; set; }
        public Guid BrandId { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public string? SocialAccountName { get; set; }
        public string? BrandName { get; set; }
        public string? ProfileName { get; set; }
    }
}
