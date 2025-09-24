using AISAM.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories
{
    public class AISAMContext : DbContext
    {
        public AISAMContext(DbContextOptions<AISAMContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<SocialAccount> SocialAccounts { get; set; }
        public DbSet<SocialTarget> SocialTargets { get; set; }
        public DbSet<SocialPost> Posts { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationMember> OrganizationMembers { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // SocialAccount configuration
            modelBuilder.Entity<SocialAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ProviderUserId).HasMaxLength(255).IsRequired();
                entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
                entity.HasIndex(e => e.UserId);
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.SocialAccounts)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SocialTarget configuration
            modelBuilder.Entity<SocialTarget>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProviderTargetId).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => new { e.SocialAccountId, e.ProviderTargetId }).IsUnique();
                
                entity.HasOne(e => e.SocialAccount)
                    .WithMany(sa => sa.SocialTargets)
                    .HasForeignKey(e => e.SocialAccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Post configuration
            modelBuilder.Entity<SocialPost>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Provider).HasMaxLength(50);
                entity.Property(e => e.ProviderPostId).HasMaxLength(255);
                entity.HasIndex(e => e.ProviderPostId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SocialAccountId);
                entity.HasIndex(e => e.SocialTargetId);
                entity.HasIndex(e => e.ScheduleId);
                entity.HasIndex(e => e.ScheduledTime);
                entity.HasIndex(e => e.Status);
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Posts)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.SocialAccount)
                    .WithMany(sa => sa.Posts)
                    .HasForeignKey(e => e.SocialAccountId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(e => e.SocialTarget)
                    .WithMany(st => st.Posts)
                    .HasForeignKey(e => e.SocialTargetId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Schedule)
                    .WithMany()
                    .HasForeignKey(e => e.ScheduleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Organization configuration
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.BillingInfo).HasColumnType("jsonb");
                entity.HasMany(e => e.Members)
                    .WithOne(m => m.Organization)
                    .HasForeignKey(m => m.OrgId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OrganizationMember configuration
            modelBuilder.Entity<OrganizationMember>(entity =>
            {
                entity.HasKey(e => new { e.OrgId, e.UserId });
                entity.Property(e => e.Role).HasMaxLength(50);
            });

            // Brand configuration
            modelBuilder.Entity<Brand>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.TargetAudience).HasColumnType("jsonb");
                entity.Property(e => e.BrandGuidelines).HasColumnType("jsonb");
                entity.HasMany(e => e.Products)
                    .WithOne(p => p.Brand)
                    .HasForeignKey(p => p.BrandId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Sku).HasMaxLength(100);
                entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("USD");
                entity.Property(e => e.Metadata).HasColumnType("jsonb");
            });

            // Schedule configuration
            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ScheduledAt).IsRequired();
                entity.Property(e => e.PublishWindow).HasColumnType("jsonb");
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("scheduled");
            });

            // AdminAuditLog configuration
            modelBuilder.Entity<AdminAuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).HasMaxLength(200).IsRequired();
                entity.Property(e => e.TargetType).HasMaxLength(100);
                entity.Property(e => e.Details).HasColumnType("jsonb");
                entity.HasIndex(e => e.AdminUserId);
                entity.HasIndex(e => e.TargetType);
                entity.HasIndex(e => e.TargetId);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasOne(e => e.AdminUser)
                    .WithMany()
                    .HasForeignKey(e => e.AdminUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}