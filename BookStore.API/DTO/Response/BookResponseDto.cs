namespace BookStore.API.DTO.Response
{
    public class BookResponseDto
    {
        public long Id { get; set; }
        public string Isbn { get; set; } = string.Empty;
        public string? Cover { get; set; }
        public string? Description { get; set; }
        public float? Discount { get; set; }
        public int? Page { get; set; }
        public long? Price { get; set; }
        public long? SalePrice { get; set; }
        public DateOnly? PublicationDate { get; set; }
        public string? Size { get; set; }
        public int? Sold { get; set; }
        public string? State { get; set; }
        public int? Stock { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? Weight { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
