using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model2
{
    [Table("performance_reports")]
    public class PerformanceReport
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("post_id")]
        public Guid PostId { get; set; }

        [Column("impressions")]
        public long Impressions { get; set; } = 0;

        [Column("engagement")]
        public long Engagement { get; set; } = 0; // Likes + comments + shares

        [Column("ctr", TypeName = "decimal(5,4)")]
        public decimal Ctr { get; set; } = 0; // Click-Through Rate

        [Column("estimated_revenue", TypeName = "decimal(10,2)")]
        public decimal EstimatedRevenue { get; set; } = 0;

        [Required]
        [Column("report_date", TypeName = "date")]
        public DateTime ReportDate { get; set; }

        [Column("raw_data", TypeName = "jsonb")]
        public string? RawData { get; set; } // JSON data from API

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;
    }
}
