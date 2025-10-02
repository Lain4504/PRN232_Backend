using AISAM.Common.Models;

namespace AISAM.Common.Dtos.Response;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SocialAccountDto> SocialAccounts { get; set; } = new();
}