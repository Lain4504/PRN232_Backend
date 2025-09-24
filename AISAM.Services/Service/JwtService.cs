using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AISAM.Data.Model;
using AISAM.Repositories;
using AISAM.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AISAM.Services.Service
{
    public class JwtService : IJwtService
    {
        private readonly AISAMContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public JwtService(AISAMContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _secretKey = _configuration["Jwt:SecretKey"] ?? throw new ArgumentNullException("Jwt:SecretKey");
            _issuer = _configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
            _audience = _configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience");
            _accessTokenExpirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
            _refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        }

        public string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var jti = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("isActive", user.IsActive.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secretKey)),
                ValidateLifetime = false, // Don't validate lifetime here
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                
                if (validatedToken is JwtSecurityToken jwtToken && 
                    jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return principal;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            return await _context.BlacklistedTokens
                .AnyAsync(bt => bt.Token == token);
        }

        public bool IsTokenBlacklisted(string token)
        {
            return _context.BlacklistedTokens
                .Any(bt => bt.Token == token);
        }

        public async Task<string> BlacklistTokenAsync(string token, string? reason = null, string? userId = null)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            var jti = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var exp = principal?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            DateTime? expiresAt = null;
            if (long.TryParse(exp, out var expUnix))
            {
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).DateTime;
            }

            var blacklistedToken = new BlacklistedToken
            {
                Token = token,
                Reason = reason,
                UserId = userId,
                Jti = jti,
                ExpiresAt = expiresAt
            };

            _context.BlacklistedTokens.Add(blacklistedToken);
            await _context.SaveChangesAsync();

            return blacklistedToken.Id.ToString();
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, string userId)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == Guid.Parse(userId));

            return token != null && token.IsActive;
        }

        public async Task<string> RevokeRefreshTokenAsync(string refreshToken, string? revokedByIp = null, string? reason = null)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
                return string.Empty;

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = revokedByIp;
            token.ReasonRevoked = reason;

            await _context.SaveChangesAsync();
            return token.Id.ToString();
        }

        public string? GetUserIdFromToken(string token)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public string? GetJtiFromToken(string token)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            return principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        }

        public DateTime? GetExpirationFromToken(string token)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            var exp = principal?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
            
            if (long.TryParse(exp, out var expUnix))
            {
                return DateTimeOffset.FromUnixTimeSeconds(expUnix).DateTime;
            }

            return null;
        }
    }
}
