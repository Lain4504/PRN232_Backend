using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model2
{
    [Table("ad_sets")]
    public class AdSet
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("campaign_id")]
        public Guid CampaignId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("targeting", TypeName = "jsonb")]
        public string? Targeting { get; set; } // JSON targeting configuration

        [Column("daily_budget", TypeName = "decimal(10,2)")]
        public decimal? DailyBudget { get; set; }

        [Column("start_date", TypeName = "date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date", TypeName = "date")]
        public DateTime? EndDate { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CampaignId")]
        public virtual AdCampaign Campaign { get; set; } = null!;

        public virtual ICollection<Ad> Ads { get; set; } = new List<Ad>();
    }
}
