using BookStore.Services.IServices;
using BookStore.Data;
using BookStore.Data.Model;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace BookStore.Services.Service
{
    public class AuthService : IAuthService
    {
        private readonly BookStoreDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly int _refreshTokenExpirationDays;

        public AuthService(BookStoreDbContext context, IJwtService jwtService, IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        }

        public async Task<(bool Success, User? User, string Message)> LoginAsync(string email, string password, string? ipAddress = null)
        {
            // Tìm user bằng email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return (false, null, "Invalid credentials");
            }

            // Kiểm tra status
            if (user.Status != UserStatusEnum.Active)
            {
                return (false, null, "Account is not active");
            }

            // Validate password
            if (!await ValidatePasswordAsync(password, user.PasswordHash))
            {
                return (false, null, "Invalid credentials");
            }

            return (true, user, "Login successful");
        }

        public async Task<(bool Success, string? AccessToken, string? RefreshToken, string Message)> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
        {
            var token = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null || !token.IsActive)
            {
                return (false, null, null, "Invalid refresh token");
            }

            if (token.User.Status != UserStatusEnum.Active)
            {
                return (false, null, null, "Account is not active");
            }

            // Revoke old refresh token
            await _jwtService.RevokeRefreshTokenAsync(refreshToken, ipAddress, "Replaced by new token");

            // Generate new tokens
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var newAccessToken = _jwtService.GenerateAccessToken(token.User);

            // Save new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = token.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                ReplacedByToken = newRefreshToken
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);

            // Update old token's replaced by token
            token.ReplacedByToken = newRefreshToken;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = "Replaced by new token";

            await _context.SaveChangesAsync();

            return (true, newAccessToken, newRefreshToken, "Token refreshed successfully");
        }

        public async Task<bool> LogoutAsync(string refreshToken, string? ipAddress = null)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
                return false;

            await _jwtService.RevokeRefreshTokenAsync(refreshToken, ipAddress, "User logout");
            return true;
        }

        public async Task<bool> LogoutAllAsync(string userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.ReasonRevoked = "Logout all devices";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokeTokenAsync(string token, string? ipAddress = null, string? reason = null)
        {
            // Check if it's a refresh token
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken != null)
            {
                await _jwtService.RevokeRefreshTokenAsync(token, ipAddress, reason);
                return true;
            }

            // If it's an access token, blacklist it
            var userId = _jwtService.GetUserIdFromToken(token);
            await _jwtService.BlacklistTokenAsync(token, reason, userId);
            return true;
        }

        public async Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(string userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ValidatePasswordAsync(string password, string passwordHash)
        {
            return await Task.Run(() =>
            {
                using var hmac = new HMACSHA512(Convert.FromBase64String(passwordHash.Split(':')[1]));
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                var storedHash = Convert.FromBase64String(passwordHash.Split(':')[0]);
                
                return computedHash.SequenceEqual(storedHash);
            });
        }

        public async Task<string> HashPasswordAsync(string password)
        {
            return await Task.Run(() =>
            {
                using var hmac = new HMACSHA512();
                var salt = hmac.Key;
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                
                return Convert.ToBase64String(hash) + ":" + Convert.ToBase64String(salt);
            });
        }

        public async Task<RefreshToken> SaveRefreshTokenAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }
    }
}
