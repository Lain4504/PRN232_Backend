using System.ComponentModel.DataAnnotations;

namespace BookStore.API.DTO.Request
{
    public class CollectionRequestDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Type { get; set; }

        public bool IsDisplay { get; set; } = true;
    }
}
