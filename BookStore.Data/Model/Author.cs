using System.ComponentModel.DataAnnotations;

namespace BookStore.Data.Model
{
    public partial class Author
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
    }
}