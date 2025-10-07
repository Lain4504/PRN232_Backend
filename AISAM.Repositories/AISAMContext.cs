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
        public DbSet<Ad> Ads { get; set; }
        public DbSet<AdCampaign> AdCampaigns { get; set; }
        public DbSet<AdSet> AdSets { get; set; }
        public DbSet<AdCreative> AdCreatives { get; set; }
        public DbSet<PerformanceReport> PerformanceReports { get; set; }
        public DbSet<ContentCalendar> ContentCalendars { get; set; }
        public DbSet<AiGeneration> AiGenerations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ContentTemplate> ContentTemplates { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

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

            // Brand entity configuration
            modelBuilder.Entity<Brand>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.HasIndex(b => b.UserId);
                entity.HasIndex(b => b.Name);
                entity.HasOne(b => b.User)
                      .WithMany(u => u.Brands)
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(b => b.Profile)
                      .WithMany()
                      .HasForeignKey(b => b.ProfileId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Content entity configuration
            modelBuilder.Entity<Content>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.AdType).HasConversion<int>();
                entity.Property(c => c.Status).HasConversion<int>().HasDefaultValue(ContentStatusEnum.Draft);
                entity.HasIndex(c => c.BrandId);
                entity.HasIndex(c => c.ProductId);
                entity.HasIndex(c => c.Status);
                entity.HasIndex(c => c.CreatedAt);
                entity.HasOne(c => c.Brand)
                      .WithMany(b => b.Contents)
                      .HasForeignKey(c => c.BrandId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(c => c.Product)
                      .WithMany(p => p.Contents)
                      .HasForeignKey(c => c.ProductId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Product entity configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.BrandId);
                entity.HasIndex(p => p.Name);
                entity.HasOne(p => p.Brand)
                      .WithMany(b => b.Products)
                      .HasForeignKey(p => p.BrandId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // SocialAccount entity configuration
            modelBuilder.Entity<SocialAccount>(entity =>
            {
                entity.HasKey(sa => sa.Id);
                entity.Property(sa => sa.Platform).HasConversion<int>();
                entity.HasIndex(sa => sa.UserId);
                entity.HasIndex(sa => sa.Platform);
                entity.HasIndex(sa => sa.AccountId);
                entity.HasIndex(sa => sa.IsActive);
                entity.HasOne(sa => sa.User)
                      .WithMany(u => u.SocialAccounts)
                      .HasForeignKey(sa => sa.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // SocialIntegration entity configuration
            modelBuilder.Entity<SocialIntegration>(entity =>
            {
                entity.HasKey(si => si.Id);
                entity.HasIndex(si => si.BrandId);
                entity.HasIndex(si => si.SocialAccountId);
                entity.HasOne(si => si.Brand)
                      .WithMany(b => b.SocialIntegrations)
                      .HasForeignKey(si => si.BrandId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(si => si.SocialAccount)
                      .WithMany(sa => sa.SocialIntegrations)
                      .HasForeignKey(si => si.SocialAccountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Post entity configuration
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Status).HasConversion<int>().HasDefaultValue(ContentStatusEnum.Published);
                entity.HasIndex(p => p.ContentId);
                entity.HasIndex(p => p.IntegrationId);
                entity.HasIndex(p => p.PublishedAt);
                entity.HasIndex(p => p.ExternalPostId);
                entity.HasOne(p => p.Content)
                      .WithMany(c => c.Posts)
                      .HasForeignKey(p => p.ContentId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.Integration)
                      .WithMany()
                      .HasForeignKey(p => p.IntegrationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Asset entity configuration
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.AssetType).HasConversion<int>();
                entity.HasIndex(a => a.UploadedBy);
                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UploadedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Profile entity configuration
            modelBuilder.Entity<Profile>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.ProfileType).HasConversion<int>();
                entity.HasIndex(p => p.UserId);
                entity.HasOne(p => p.User)
                      .WithMany(u => u.Profiles)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Team entity configuration
            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => t.VendorId);
                entity.HasIndex(t => t.Name);
                entity.HasOne(t => t.Vendor)
                      .WithMany(u => u.Teams)
                      .HasForeignKey(t => t.VendorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TeamMember entity configuration
            modelBuilder.Entity<TeamMember>(entity =>
            {
                entity.HasKey(tm => tm.Id);
                entity.HasIndex(tm => tm.TeamId);
                entity.HasIndex(tm => tm.UserId);
                entity.HasOne(tm => tm.Team)
                      .WithMany(t => t.TeamMembers)
                      .HasForeignKey(tm => tm.TeamId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(tm => tm.User)
                      .WithMany(u => u.TeamMembers)
                      .HasForeignKey(tm => tm.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Subscription entity configuration
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Plan).HasConversion<int>();
                entity.HasIndex(s => s.UserId);
                entity.HasIndex(s => s.IsActive);
                entity.HasOne(s => s.User)
                      .WithMany(u => u.Subscriptions)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Approval entity configuration
            modelBuilder.Entity<Approval>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => a.ContentId);
                entity.HasIndex(a => a.ApproverId);
                entity.HasOne(a => a.Content)
                      .WithMany(c => c.Approvals)
                      .HasForeignKey(a => a.ContentId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Approver)
                      .WithMany(u => u.Approvals)
                      .HasForeignKey(a => a.ApproverId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            // AdCampaign entity configuration
            modelBuilder.Entity<AdCampaign>(entity =>
            {
                entity.HasKey(ac => ac.Id);
                entity.HasIndex(ac => ac.UserId);
                entity.HasIndex(ac => ac.BrandId);
                entity.HasIndex(ac => ac.Name);
                entity.HasOne(ac => ac.User)
                      .WithMany(u => u.AdCampaigns)
                      .HasForeignKey(ac => ac.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ac => ac.Brand)
                      .WithMany(b => b.AdCampaigns)
                      .HasForeignKey(ac => ac.BrandId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AdSet entity configuration
            modelBuilder.Entity<AdSet>(entity =>
            {
                entity.HasKey(ads => ads.Id);
                entity.HasIndex(ads => ads.CampaignId);
                entity.HasOne(ads => ads.Campaign)
                      .WithMany()
                      .HasForeignKey(ads => ads.CampaignId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AdCreative entity configuration
            modelBuilder.Entity<AdCreative>(entity =>
            {
                entity.HasKey(adc => adc.Id);
                entity.HasIndex(adc => adc.ContentId);
                entity.HasOne(adc => adc.Content)
                      .WithMany(c => c.AdCreatives)
                      .HasForeignKey(adc => adc.ContentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Ad entity configuration
            modelBuilder.Entity<Ad>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => a.AdSetId);
                entity.HasIndex(a => a.CreativeId);
                entity.HasOne(a => a.AdSet)
                      .WithMany(ads => ads.Ads)
                      .HasForeignKey(a => a.AdSetId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Creative)
                      .WithMany(adc => adc.Ads)
                      .HasForeignKey(a => a.CreativeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // PerformanceReport entity configuration
            modelBuilder.Entity<PerformanceReport>(entity =>
            {
                entity.HasKey(pr => pr.Id);
                entity.HasIndex(pr => pr.PostId);
                entity.HasIndex(pr => pr.ReportDate);
                entity.HasOne(pr => pr.Post)
                      .WithMany(p => p.PerformanceReports)
                      .HasForeignKey(pr => pr.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ContentCalendar entity configuration
            modelBuilder.Entity<ContentCalendar>(entity =>
            {
                entity.HasKey(cc => cc.Id);
                entity.HasIndex(cc => cc.ContentId);
                entity.HasIndex(cc => cc.ScheduledDate);
                entity.HasOne(cc => cc.Content)
                      .WithMany(c => c.ContentCalendars)
                      .HasForeignKey(cc => cc.ContentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AiGeneration entity configuration
            modelBuilder.Entity<AiGeneration>(entity =>
            {
                entity.HasKey(ag => ag.Id);
                entity.Property(ag => ag.Status).HasConversion<int>().HasDefaultValue(AiStatusEnum.Pending);
                entity.HasIndex(ag => ag.ContentId);
                entity.HasIndex(ag => ag.Status);
                entity.HasOne(ag => ag.Content)
                      .WithMany(c => c.AiGenerations)
                      .HasForeignKey(ag => ag.ContentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Notification entity configuration
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Type).HasConversion<int>();
                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => n.Type);
                entity.HasIndex(n => n.IsRead);
                entity.HasIndex(n => n.CreatedAt);
                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Payment entity configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Status).HasConversion<int>().HasDefaultValue(PaymentStatusEnum.Pending);
                entity.Property(p => p.Amount).HasPrecision(10, 2);
                entity.HasIndex(p => p.UserId);
                entity.HasIndex(p => p.Status);
                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.Subscription)
                      .WithMany()
                      .HasForeignKey(p => p.SubscriptionId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ContentTemplate entity configuration
            modelBuilder.Entity<ContentTemplate>(entity =>
            {
                entity.HasKey(ct => ct.Id);
                entity.HasIndex(ct => ct.BrandId);
                entity.HasIndex(ct => ct.TemplateType);
                entity.HasIndex(ct => ct.IsActive);
                entity.HasOne(ct => ct.Brand)
                      .WithMany()
                      .HasForeignKey(ct => ct.BrandId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditLog entity configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(al => al.Id);
                entity.HasIndex(al => al.ActorId);
                entity.HasIndex(al => al.TargetTable);
                entity.HasIndex(al => al.CreatedAt);
                entity.HasOne(al => al.Actor)
                      .WithMany()
                      .HasForeignKey(al => al.ActorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}