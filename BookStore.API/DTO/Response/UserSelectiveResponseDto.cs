namespace BookStore.API.DTO.Response
{
    public class UserSelectiveResponseDto
    {
        public string Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        // Không có FullName, PhoneNumber, Address, CreatedAt, UpdatedAt
    }
}
