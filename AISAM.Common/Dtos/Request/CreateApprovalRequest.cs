using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Request
{
    public class CreateApprovalRequest
    {
        public Guid ContentId { get; set; }
        public Guid ApproverId { get; set; }
        public string? Notes { get; set; }
    }
}