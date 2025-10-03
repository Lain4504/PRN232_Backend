using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("ai_generations")]
    public class AiGeneration
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("content_id")]
        public Guid ContentId { get; set; }

        [Required]
        [Column("ai_prompt")]
        public string AiPrompt { get; set; } = string.Empty;

        [Column("generated_text")]
        public string? GeneratedText { get; set; }

        [MaxLength(500)]
        [Column("generated_image_url")]
        public string? GeneratedImageUrl { get; set; }

        [MaxLength(500)]
        [Column("generated_video_url")]
        public string? GeneratedVideoUrl { get; set; }

        [Required]
        [Column("status")]
        public AiStatusEnum Status { get; set; } = AiStatusEnum.Pending;

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ContentId")]
        public virtual Content Content { get; set; } = null!;
    }
}
