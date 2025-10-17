using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model
{
    [Table("ad_campaigns")]
    public class AdCampaign
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("brand_id")]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("ad_account_id")]
        public string AdAccountId { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("facebook_campaign_id")]
        public string? FacebookCampaignId { get; set; } // Facebook Campaign ID from Marketing API

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("objective")]
        public string? Objective { get; set; }

        [Column("budget", TypeName = "decimal(10,2)")]
        public decimal? Budget { get; set; }

        [Column("start_date", TypeName = "date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date", TypeName = "date")]
        public DateTime? EndDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("BrandId")]
        public virtual Brand Brand { get; set; } = null!;

        public virtual ICollection<AdSet> AdSets { get; set; } = new List<AdSet>();
    }
}
