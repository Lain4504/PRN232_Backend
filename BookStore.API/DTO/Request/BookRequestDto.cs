using System.ComponentModel.DataAnnotations;

namespace BookStore.API.DTO.Request
{
    public class BookRequestDto
    {
        [Required]
        [MaxLength(20)]
        public string Isbn { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Cover { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public float? Discount { get; set; }

        public int? Page { get; set; }

        public long? Price { get; set; }

        public DateOnly? PublicationDate { get; set; }

        [MaxLength(50)]
        public string? Size { get; set; }

        public int? Stock { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public int? Weight { get; set; }
    }
}
