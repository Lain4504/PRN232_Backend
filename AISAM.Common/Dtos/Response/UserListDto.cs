namespace AISAM.Common.Dtos.Response;

public class UserListDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int SocialAccountsCount { get; set; }
}