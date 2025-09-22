using System.ComponentModel.DataAnnotations;

namespace BookStore.API.DTO.Request
{
    public class CreateAuthorRequestDto
    {
        [Required(ErrorMessage = "Author name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Author name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;
    }
}