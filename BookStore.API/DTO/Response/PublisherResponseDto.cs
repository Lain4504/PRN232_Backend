namespace BookStore.API.DTO.Response
{
    public class PublisherResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Website { get; set; }
    }
}