namespace BookStore.API.DTO.Request;

public class CreateWishlistRequestDto
{
    public BookStore.Data.Model.Book? Book { get; set; }
    public string UserId { get; set; } = string.Empty;
}
