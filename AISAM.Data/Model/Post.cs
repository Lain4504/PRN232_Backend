using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("posts")]
    public class Post
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("content_id")]
        public Guid ContentId { get; set; }

        [Required]
        [Column("integration_id")]
        public Guid IntegrationId { get; set; }

        [MaxLength(255)]
        [Column("external_post_id")]
        public string? ExternalPostId { get; set; }

        [Required]
        [Column("published_at")]
        public DateTime PublishedAt { get; set; }

        [Required]
        [Column("status")]
        public ContentStatusEnum Status { get; set; } = ContentStatusEnum.Published;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ContentId")]
        public virtual Content Content { get; set; } = null!;

        [ForeignKey("IntegrationId")]
        public virtual SocialIntegration Integration { get; set; } = null!;

        public virtual ICollection<PerformanceReport> PerformanceReports { get; set; } = new List<PerformanceReport>();
    }
}
