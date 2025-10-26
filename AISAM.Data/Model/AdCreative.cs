using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model
{
    [Table("ad_creatives")]
    public class AdCreative
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("content_id")]
        public Guid? ContentId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("ad_account_id")]
        public string AdAccountId { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("creative_id")]
        public string? CreativeId { get; set; }

        [MaxLength(50)]
        [Column("call_to_action")]
        public string? CallToAction { get; set; }

        [MaxLength(255)]
        [Column("facebook_post_id")]
        public string? FacebookPostId { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ContentId")]
        public virtual Content? Content { get; set; }

        public virtual ICollection<Ad> Ads { get; set; } = new List<Ad>();
    }
}
