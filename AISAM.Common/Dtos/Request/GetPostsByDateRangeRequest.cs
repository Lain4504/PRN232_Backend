using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class GetPostsByDateRangeRequest
    {
        [Required(ErrorMessage = "StartDate là bắt buộc")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "EndDate là bắt buộc")]
        public DateTime EndDate { get; set; }

        public Guid? ContentId { get; set; }
        public Guid? IntegrationId { get; set; }
    }
}