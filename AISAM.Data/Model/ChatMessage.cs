using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model
{
    [Table("chat_messages")]
    public class ChatMessage
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("conversation_id")]
        public Guid ConversationId { get; set; }

        [Required]
        [Column("sender_type")]
        public ChatSenderType SenderType { get; set; }

        [Required]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("ai_generation_id")]
        public Guid? AiGenerationId { get; set; }

        [Column("content_id")]
        public Guid? ContentId { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; } = null!;

        [ForeignKey("AiGenerationId")]
        public virtual AiGeneration? AiGeneration { get; set; }

        [ForeignKey("ContentId")]
        public virtual Content? Content { get; set; }
    }

    public enum ChatSenderType
    {
        User = 0,
        AI = 1
    }
}