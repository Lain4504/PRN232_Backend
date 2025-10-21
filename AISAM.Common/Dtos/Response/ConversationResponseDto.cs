namespace AISAM.Common.Dtos.Response
{
    public class ConversationResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public Guid? BrandId { get; set; }
        public string? BrandName { get; set; }
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string AdType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int MessageCount { get; set; }
    }
}