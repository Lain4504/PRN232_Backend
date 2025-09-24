using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
    public class RefreshToken
    {
        [Key]
        [Required]
        public Guid Id { get; set; } 

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; set; }

        public string? RevokedByIp { get; set; }

        public string? ReplacedByToken { get; set; }

        public string? ReasonRevoked { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        public bool IsRevoked => RevokedAt.HasValue;

        public bool IsActive => !IsRevoked && !IsExpired;

        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}
