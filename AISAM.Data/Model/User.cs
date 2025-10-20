using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [Column("role")]
        public UserRoleEnum Role { get; set; } = UserRoleEnum.User;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Profile> Profiles { get; set; } = new List<Profile>();
        public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
        public virtual ICollection<SocialAccount> SocialAccounts { get; set; } = new List<SocialAccount>();
        public virtual ICollection<SocialIntegration> SocialIntegrations { get; set; } = new List<SocialIntegration>();
        public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();
        public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
        public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public virtual ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    }
}
