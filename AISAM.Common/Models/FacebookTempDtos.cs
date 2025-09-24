using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Models
{
    // [TEMP] Facebook-only DTOs to support the temporary token-based linking flow.
    // Will be removed when the official OAuth flow replaces /auth/link-page-token.
    public class LinkPageByTokenRequest
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public string PageAccessToken { get; set; } = string.Empty;
        
        public string? UserAccessToken { get; set; }
    }
}


