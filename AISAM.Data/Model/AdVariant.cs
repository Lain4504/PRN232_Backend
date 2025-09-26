using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model
{
    public class AdVariant
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Creative")] 
        public Guid? CreativeId { get; set; }

        [MaxLength(100)]
        public string? VariantKey { get; set; }

        // JSONB columns represented as text in the entity
        public string? Assets { get; set; }

        public string? Copy { get; set; }

        [MaxLength(100)]
        public string? CallToAction { get; set; }

        public string? TargetSpec { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "inactive";
    }
}


