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
        public int QuotaPostsPerMonth { get; set; } = 100;

        [Column("quota_storage_gb")]
        public int QuotaStorageGb { get; set; } = 5;

        [Column("quota_ad_budget_monthly", TypeName = "decimal(10,2)")]
        public decimal QuotaAdBudgetMonthly { get; set; } = 100.00m;

        [Column("quota_ad_campaigns")]
        public int QuotaAdCampaigns { get; set; } = 5;

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
        [Column("stripe_subscription_id")]
        public string? StripeSubscriptionId { get; set; }

        [MaxLength(255)]
        [Column("stripe_customer_id")]
        public string? StripeCustomerId { get; set; }

        // Navigation properties
        [ForeignKey("ProfileId")]
        public virtual Profile Profile { get; set; } = null!;
    }
}
