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
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure OrganizationMember composite primary key
            modelBuilder.Entity<OrganizationMember>()
                .HasKey(om => new { om.OrgId, om.UserId });

            // Configure foreign key relationships for OrganizationMember
            modelBuilder.Entity<OrganizationMember>()
                .HasOne(om => om.Organization)
                .WithMany()
                .HasForeignKey(om => om.OrgId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}