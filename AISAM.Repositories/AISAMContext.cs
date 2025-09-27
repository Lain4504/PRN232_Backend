using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories
{
    public class AisamContext : DbContext
    {
        public AisamContext(DbContextOptions<AisamContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<SocialAccount> SocialAccounts { get; set; }
        public DbSet<SocialIntegration> SocialIntegrations { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Content> Contents { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Approval> Approvals { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<Ad> Ads { get; set; }
        public DbSet<AdCampaign> AdCampaigns { get; set; }
        public DbSet<AdSet> AdSets { get; set; }
        public DbSet<AdCreative> AdCreatives { get; set; }
        public DbSet<PerformanceReport> PerformanceReports { get; set; }
        public DbSet<ContentCalendar> ContentCalendars { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity indexes and constraints (Supabase-auth anchored)
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).HasMaxLength(255);
                entity.HasIndex(u => u.Email);
                entity.Property(u => u.Role).HasConversion<int>().HasDefaultValue(UserRoleEnum.User);
                entity.HasIndex(u => u.Role);
            });

			modelBuilder.Entity<Asset>()
				.HasOne(a => a.User)
				.WithMany()
				.HasForeignKey(a => a.UploadedBy)
				.OnDelete(DeleteBehavior.SetNull);
        }
    }
}