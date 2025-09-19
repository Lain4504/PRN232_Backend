using System.ComponentModel.DataAnnotations;

namespace BookStore.API.DTO.Request
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email or Username is required")]
        [StringLength(256, ErrorMessage = "Email or Username cannot exceed 256 characters")]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
