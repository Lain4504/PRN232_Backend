namespace AISAM.Common.Dtos.Request
{
    public class CreateTeamRequest
    {
        // Admin may specify VendorId; Vendors cannot override and it will be set to current user
        public Guid? VendorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
