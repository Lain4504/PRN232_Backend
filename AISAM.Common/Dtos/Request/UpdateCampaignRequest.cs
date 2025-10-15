using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class UpdateCampaignRequest
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Objective { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Budget { get; set; }

        public bool? IsActive { get; set; }
    }
}


