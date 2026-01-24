namespace AISAM.Common.Config
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 60; // 1 hour
        public int RefreshTokenExpirationDays { get; set; } = 30; // 30 days
    }
}
