using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class ProfileResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ProfileTypeEnum ProfileType { get; set; }
        public Guid? SubscriptionId { get; set; }
        public string? CompanyName { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public ProfileStatusEnum Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}