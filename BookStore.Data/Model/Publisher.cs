using System.ComponentModel.DataAnnotations;

namespace BookStore.Data.Model
{
    public partial class Publisher
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Website { get; set; }

        // Navigation property for related books (commented out until Book entity is implemented)
        // public virtual ICollection<Book>? Books { get; set; }
    }
}