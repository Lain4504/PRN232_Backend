using System.ComponentModel.DataAnnotations;
using BookStore.Common.Enumeration;

namespace BookStore.API.DTO.Request
{
    public class CreateUserRequestDto
    {
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters")]
        public string? FullName { get; set; }

        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }

        [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        public UserRoleEnum Role { get; set; } = UserRoleEnum.Customer;
    }
}
