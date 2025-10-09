using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class UpdatePostRequest
    {
        [Required(ErrorMessage = "ContentId là bắt buộc")]
        public Guid ContentId { get; set; }

        [Required(ErrorMessage = "IntegrationId là bắt buộc")]
        public Guid IntegrationId { get; set; }

        [MaxLength(255, ErrorMessage = "ExternalPostId không được vượt quá 255 ký tự")]
        public string? ExternalPostId { get; set; }

        [Required(ErrorMessage = "PublishedAt là bắt buộc")]
        public DateTime PublishedAt { get; set; }
    }
}