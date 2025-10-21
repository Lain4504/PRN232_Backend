using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("profiles")]
    public class Profile
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("profile_type")]
        public ProfileTypeEnum ProfileType { get; set; }

        [Column("subscription_id")]
        public Guid? SubscriptionId { get; set; }

        [MaxLength(255)]
        [Column("company_name")]
        public string? CompanyName { get; set; }

        [Column("bio")]
        public string? Bio { get; set; }

        [MaxLength(500)]
        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("SubscriptionId")]
        public virtual Subscription? Subscription { get; set; }

        public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
        public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
        public virtual ICollection<SocialAccount> SocialAccounts { get; set; } = new List<SocialAccount>();
        public virtual ICollection<SocialIntegration> SocialIntegrations { get; set; } = new List<SocialIntegration>();
        public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();
        public virtual ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
        public virtual ICollection<ContentCalendar> ContentCalendars { get; set; } = new List<ContentCalendar>();
    }
}
