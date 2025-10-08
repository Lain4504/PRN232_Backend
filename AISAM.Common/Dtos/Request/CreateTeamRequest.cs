namespace AISAM.Common.Dtos.Request
{
    public class CreateTeamRequest
    {
        // Admin may specify VendorId; Vendors cannot override and it will be set to current user
        [Required]
        public Guid? VendorId { get; set; }
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
