namespace BookStore.API.DTO.Response
{
    public class CollectionResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public bool IsDisplay { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
