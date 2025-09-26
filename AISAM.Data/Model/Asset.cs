using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model
{
    public class Asset
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Organization")] 
        public Guid? OrganizationId { get; set; }

        [ForeignKey("User")] 
        public Guid? UploadedBy { get; set; }

        [MaxLength(20)]
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string StoragePath { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MimeType { get; set; }

        public long? SizeBytes { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public decimal? DurationSeconds { get; set; }

        // Store JSON as text; database column should be jsonb in PostgreSQL
        public string? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Organization? Organization { get; set; }
        public virtual User? User { get; set; }
    }
}


