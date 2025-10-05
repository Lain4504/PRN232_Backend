using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Request
{
    public class UpdateApprovalRequest
    {
        public ContentStatusEnum Status { get; set; }
        public string? Notes { get; set; }
    }
}