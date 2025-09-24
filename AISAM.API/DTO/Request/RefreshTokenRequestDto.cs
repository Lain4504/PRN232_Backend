using System.ComponentModel.DataAnnotations;

namespace BookStore.API.DTO.Request
{
    public class RefreshTokenRequestDto
    {
        [Required(ErrorMessage = "Refresh token is required")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Refresh token must be between 1 and 500 characters")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
