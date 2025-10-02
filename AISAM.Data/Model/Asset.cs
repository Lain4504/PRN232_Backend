using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("assets")]
    public class Asset
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [ForeignKey("User")] 
        [Column("uploaded_by")]
        public Guid? UploadedBy { get; set; }

        [MaxLength(20)]
        [Required]
        [Column("type")]
        public AssetTypeEnum AssetType { get; set; }

        [Required]
        [Column("storage_path")]
        public string StoragePath { get; set; } = string.Empty; // supabase storage path or URL

        [MaxLength(100)]
        [Column("mime_type")]
        public string? MimeType { get; set; }

        [Column("size_bytes")]
        public long? SizeBytes { get; set; }

        [Column("width")]
        public int? Width { get; set; }

        [Column("height")]
        public int? Height { get; set; }

        [Column("duration_seconds", TypeName = "decimal(10,2)")]
        public decimal? DurationSeconds { get; set; }

        // Store JSON as text; database column should be jsonb in PostgreSQL
        [Column("metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual User? User { get; set; }
    }
}


