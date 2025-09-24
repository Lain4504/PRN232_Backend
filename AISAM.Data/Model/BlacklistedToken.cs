using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
    public class BlacklistedToken
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        public string? Reason { get; set; }

        public string? UserId { get; set; }

        // JTI (JWT ID) để tracking token
        public string? Jti { get; set; }
    }
}
