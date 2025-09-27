using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model2
{
    [Table("ads")]
    public class Ad
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("ad_set_id")]
        public Guid AdSetId { get; set; }

        [Required]
        [Column("creative_id")]
        public Guid CreativeId { get; set; }

        [MaxLength(255)]
        [Column("ad_id")]
        public string? AdId { get; set; }

        [MaxLength(50)]
        [Column("status")]
        public string? Status { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("AdSetId")]
        public virtual AdSet AdSet { get; set; } = null!;

        [ForeignKey("CreativeId")]
        public virtual AdCreative Creative { get; set; } = null!;
    }
}
