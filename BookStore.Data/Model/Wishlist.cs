using System.ComponentModel.DataAnnotations;

namespace BookStore.Data.Model
{
    public class Wishlist
    {
        [Key]
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Book? Book { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
