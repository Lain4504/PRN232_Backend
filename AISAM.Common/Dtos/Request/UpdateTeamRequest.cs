namespace AISAM.Common.Dtos.Request
{
    public class UpdateTeamRequest
    {
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
