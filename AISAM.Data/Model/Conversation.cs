using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("conversations")]
    public class Conversation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("profile_id")]
        public Guid ProfileId { get; set; }

        [Column("brand_id")]
        public Guid? BrandId { get; set; }

        [Column("product_id")]
        public Guid? ProductId { get; set; }

        [Required]
        [Column("ad_type")]
        public AdTypeEnum AdType { get; set; }

        [MaxLength(255)]
        [Column("title")]
        public string? Title { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProfileId")]
        public virtual Profile Profile { get; set; } = null!;

        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}