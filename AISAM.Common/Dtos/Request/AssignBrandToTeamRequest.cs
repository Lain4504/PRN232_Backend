namespace AISAM.Common.Dtos.Request
{
    public class AssignBrandToTeamRequest
    {
        public List<Guid> BrandIds { get; set; } = new List<Guid>();
    }
}
