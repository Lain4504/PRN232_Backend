using System.ComponentModel.DataAnnotations;

namespace BookStore.Data.Model
{
    public class Review
    {
        [Key]
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        [Required]
        public long BookId { get; set; } = 0;
        public Book? Book { get; set; }
        [Required]
        [Range(1,5)]
        public int Rating { get; set; } = 0;
        [Required]
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public List<ReviewReply>? Replies { get; set; }
    }

    public class ReviewReply
    {
        [Key]
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string ReviewId { get; set; } = string.Empty;
        public Review? Review { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        [Required]
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
