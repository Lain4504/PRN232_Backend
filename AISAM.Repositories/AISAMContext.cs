using AISAM.Data.Model;
using AISAM.Data.Model2;
using Microsoft.EntityFrameworkCore;
using SocialAccount = AISAM.Data.Model.SocialAccount;
using User = AISAM.Data.Model.User;

namespace AISAM.Repositories
{
    public class AisamContext : DbContext
    {
        public AisamContext(DbContextOptions<AisamContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<SocialAccount> SocialAccounts { get; set; }
        public DbSet<SocialTarget> SocialTargets { get; set; }
        public DbSet<SocialPost> Posts { get; set; }
		public DbSet<Asset> Assets { get; set; }

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

			modelBuilder.Entity<Asset>()
				.HasOne(a => a.User)
				.WithMany()
				.HasForeignKey(a => a.UploadedBy)
				.OnDelete(DeleteBehavior.SetNull);
        }
    }
}