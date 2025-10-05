using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class ProfileResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ProfileTypeEnum ProfileType { get; set; }
        public string? CompanyName { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}