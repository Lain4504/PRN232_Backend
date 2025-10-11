using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.DTOs.Request
{
    public class CreateTeamRequest
    {
        [Required(ErrorMessage = "Tên team là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên team không được vượt quá 255 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        // Optional: Có thể assign brand ngay khi tạo team
        public List<Guid>? BrandIds { get; set; }
    }
}