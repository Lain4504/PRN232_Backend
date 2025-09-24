namespace BookStore.API.DTO.Response
{
    public class WishlistResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public BookStore.Data.Model.Book? Book { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
