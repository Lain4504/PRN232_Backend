using System.ComponentModel.DataAnnotations;

public class TeamMemberCreateRequest
{
    [Required]
    public Guid TeamId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Role { get; set; }

    public List<string>? Permissions { get; set; }
}

public class TeamMemberUpdateRequest
{
    public string? TeamId { get; set; }
    public string? Role { get; set; }
    public List<string>? Permissions { get; set; }
    public bool? IsActive { get; set; }
}
