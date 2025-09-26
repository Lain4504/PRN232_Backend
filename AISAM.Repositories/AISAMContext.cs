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
        // Removed legacy JWT-related entities; Supabase Auth will manage tokens
		public DbSet<Asset> Assets { get; set; }
		public DbSet<AdVariant> AdVariants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity indexes and constraints (Supabase-auth anchored)
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).HasMaxLength(255);
                entity.HasIndex(u => u.Email);
                entity.Property(u => u.Role).HasMaxLength(50).HasDefaultValue("user");
                entity.HasIndex(u => u.Role);
            });

            // Configure OrganizationMember composite primary key
            modelBuilder.Entity<OrganizationMember>()
                .HasKey(om => new { om.OrgId, om.UserId });

            // Configure foreign key relationships for OrganizationMember
            modelBuilder.Entity<OrganizationMember>()
                .HasOne(om => om.Organization)
                .WithMany()
                .HasForeignKey(om => om.OrgId)
                .OnDelete(DeleteBehavior.Cascade);

			// Asset foreign keys with ON DELETE SET NULL behavior
			modelBuilder.Entity<Asset>()
				.HasOne(a => a.Organization)
				.WithMany()
				.HasForeignKey(a => a.OrganizationId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<Asset>()
				.HasOne(a => a.User)
				.WithMany()
				.HasForeignKey(a => a.UploadedBy)
				.OnDelete(DeleteBehavior.SetNull);
        }
    }
}