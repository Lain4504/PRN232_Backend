using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class TeamResponse
    {
        public Guid Id { get; set; }
        public Guid VendorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string VendorEmail { get; set; } = string.Empty;
        public TeamStatusEnum Status { get; set; } = TeamStatusEnum.Active;
        public int MembersCount { get; set; } = 0;
    }
}