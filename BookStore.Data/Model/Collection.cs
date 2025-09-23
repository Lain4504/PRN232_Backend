using System.ComponentModel.DataAnnotations;

namespace BookStore.Data.Model
{
    public class Collection
    {
        [Key]
        [Required]
        public long Id { get; set; }

        [Required]
        public bool IsDisplay { get; set; } = true;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Type { get; set; }

        // Audit fields
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
