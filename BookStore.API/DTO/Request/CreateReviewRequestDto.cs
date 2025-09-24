namespace BookStore.API.DTO.Request
{
    public class CreateReviewRequestDto
    {
        public string BookId { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class CreateReviewReplyRequestDto
    {
        public string ReviewId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
