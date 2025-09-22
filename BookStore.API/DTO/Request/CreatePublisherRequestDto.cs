using System.ComponentModel.DataAnnotations;

namespace BookStore.API.DTO.Request
{
    public class CreatePublisherRequestDto
    {
        [Required(ErrorMessage = "Publisher name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Publisher name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Website URL cannot exceed 500 characters")]
        [Url(ErrorMessage = "Invalid website URL format")]
        public string? Website { get; set; }
    }
}