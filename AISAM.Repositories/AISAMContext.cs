using BookStore.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repositories
{
    public class AISAMContext : DbContext
    {
        public AISAMContext(DbContextOptions<AISAMContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<SocialAccount> SocialAccounts { get; set; }
        public DbSet<SocialTarget> SocialTargets { get; set; }
        public DbSet<Post> Posts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
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
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Provider).HasMaxLength(50);
                entity.Property(e => e.ProviderPostId).HasMaxLength(255);
                entity.HasIndex(e => e.ProviderPostId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SocialAccountId);
                entity.HasIndex(e => e.SocialTargetId);
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
            });
        }
    }
}