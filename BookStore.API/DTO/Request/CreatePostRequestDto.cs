using System.ComponentModel.DataAnnotations;

namespace BookStore.API.DTO.Request
{
    public class CreatePostRequestDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? Brief { get; set; }

        public string? Thumbnail { get; set; } // URL từ upload-thumbnail API
    }
}