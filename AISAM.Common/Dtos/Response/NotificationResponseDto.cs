using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationTypeEnum Type { get; set; }
        public Guid? TargetId { get; set; }
        public string? TargetType { get; set; }
        public bool IsRead { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserEmail { get; set; } = string.Empty;
    }
}
