using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class PublishAdRequest
    {
        [Required]
        public Guid AdSetId { get; set; }

        [Required]
        public Guid CreativeId { get; set; }
    }
}


