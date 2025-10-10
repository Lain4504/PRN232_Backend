using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class NotificationListDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationTypeEnum Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserEmail { get; set; } = string.Empty;
    }
}
