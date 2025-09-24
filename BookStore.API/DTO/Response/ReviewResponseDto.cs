using BookStore.Data.Model;

namespace BookStore.API.DTO.Response
{
    public class ReviewResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string BookId { get; set; } = string.Empty;
        public Book? Book { get; set; }
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ReviewReplyResponseDto>? Replies { get; set; }
    }

    public class ReviewReplyResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string ReviewId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
