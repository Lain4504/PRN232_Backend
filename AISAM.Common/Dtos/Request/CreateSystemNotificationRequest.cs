using AISAM.Data.Enumeration;
using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class CreateSystemNotificationRequest
    {
        [Required]
        public NotificationTypeEnum Type { get; set; }

        [Required]
        public string TitleTemplate { get; set; } = string.Empty;

        [Required]
        public string MessageTemplate { get; set; } = string.Empty;

        public Guid? TargetId { get; set; }

        [MaxLength(50)]
        public string? TargetType { get; set; }

        // Thông tin để xác định receivers
        public Guid? ContentId { get; set; } // Để tìm owner của content
        public Guid? BrandId { get; set; } // Để tìm user_id của brand
        public Guid? ApproverId { get; set; } // Approver cụ thể
        public List<Guid>? TeamMemberIds { get; set; } // Danh sách team members
        public bool SendToAllTeamMembers { get; set; } = false; // Gửi cho tất cả team members
        public Guid? VendorId { get; set; } // Để tìm tất cả team members của vendor cụ thể

        // Thông tin thay thế cho template
        public Dictionary<string, string>? TemplateVariables { get; set; }
    }
}
