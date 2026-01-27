using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("subscriptions")]
    public class Subscription
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("profile_id")]
        public Guid ProfileId { get; set; }

        [Required]
        [Column("plan")]
        public SubscriptionPlanEnum Plan { get; set; }

        [Column("quota_posts_per_month")]
        public int QuotaPostsPerMonth { get; set; } = 5;

        [Column("quota_ai_content_per_day")]
        public int QuotaAIContentPerDay { get; set; } = 0;

        [Column("quota_ai_images_per_day")]
        public int QuotaAIImagesPerDay { get; set; } = 0;

        [Column("quota_platforms")]
        public int QuotaPlatforms { get; set; } = 1;

        [Column("quota_accounts")]
        public int QuotaAccounts { get; set; } = 1;

        [Column("analysis_level")]
        public int AnalysisLevel { get; set; } = 0; // 0: Basic (Age/Gender), 1: Plus (Ad/Reach), 2: Premium (Plus + Recommendations)

        [Column("quota_ad_budget_monthly", TypeName = "decimal(18,2)")]
        public decimal QuotaAdBudgetMonthly { get; set; } = 0.00m;

        [Column("quota_ad_campaigns")]
        public int QuotaAdCampaigns { get; set; } = 0;

        [Required]
        [Column("start_date", TypeName = "date")]
        public DateTime StartDate { get; set; }

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

        [MaxLength(255)]
        [Column("payos_order_code")]
        public string? PayOSOrderCode { get; set; }

        [MaxLength(255)]
        [Column("payos_payment_link_id")]
        public string? PayOSPaymentLinkId { get; set; }

        // Navigation properties
        [ForeignKey("ProfileId")]
        public virtual Profile Profile { get; set; } = null!;
    }
}
