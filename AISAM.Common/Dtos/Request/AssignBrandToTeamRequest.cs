namespace AISAM.Common.DTOs.Request
{
    public class AssignBrandToTeamRequest
    {
        public Guid BrandId { get; set; }
        public bool Assign { get; set; } = true; // true để assign, false để unassign
    }
}
