using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model
{
    [Table("content_templates")]
    public class ContentTemplate
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("brand_id")]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("template_type")]
        public string TemplateType { get; set; } = string.Empty;

        [Required]
        [Column("template_data", TypeName = "jsonb")]
        public string TemplateData { get; set; } = string.Empty;

        [Column("representative_character")]
        public string? RepresentativeCharacter { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BrandId")]
        public virtual Brand Brand { get; set; } = null!;
    }
}
