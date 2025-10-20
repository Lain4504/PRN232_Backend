namespace AISAM.Common.Dtos.Response
{
    public class ConversationDetailDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? BrandId { get; set; }
        public string? BrandName { get; set; }
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string AdType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
    }

    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public string SenderType { get; set; } = string.Empty; // "User" or "AI"
        public string Message { get; set; } = string.Empty;
        public Guid? AiGenerationId { get; set; }
        public Guid? ContentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}