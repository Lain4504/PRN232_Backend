using AISAM.Data.Enumeration;
using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class UpdateNotificationRequest
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        public string? Message { get; set; }

        public NotificationTypeEnum? Type { get; set; }

        public Guid? TargetId { get; set; }

        [MaxLength(50)]
        public string? TargetType { get; set; }

        public bool? IsRead { get; set; }
    }
}
