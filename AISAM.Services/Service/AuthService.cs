using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AISAM.Common.Config;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth;


namespace AISAM.Services.Service
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IEmailService _emailService;
        private readonly JwtSettings _jwtSettings;
        private readonly GoogleSettings _googleSettings;

        public AuthService(
            IUserRepository userRepository,
            ISessionRepository sessionRepository,
            IEmailService emailService,
            IOptions<JwtSettings> jwtSettings,
            IOptions<GoogleSettings> googleSettings)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _emailService = emailService;
            _jwtSettings = jwtSettings.Value;
            _googleSettings = googleSettings.Value;
        }

        public async Task<TokenResponse> RegisterAsync(RegisterRequest request, string? userAgent, string? ipAddress)
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Create password hash and salt
            CreatePasswordHash(request.Password, out string passwordHash, out string passwordSalt);

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = UserRoleEnum.User,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            // Generate email verification token
            var verificationToken = GenerateSecureToken();
            var verificationTokenExpiration = DateTime.UtcNow.AddDays(7); // Token expires in 7 days

            user.EmailVerificationToken = verificationToken;
            user.EmailVerificationTokenExpiresAt = verificationTokenExpiration;

            await _userRepository.CreateAsync(user);

            // Send verification email
            await _emailService.SendEmailVerificationAsync(user.Email, user.FullName ?? "User", verificationToken);

            // Generate tokens
            return await GenerateTokensAsync(user, userAgent, ipAddress);
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest request, string? userAgent, string? ipAddress)
        {
            // Get user by email
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Verify password
            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            // Generate tokens
            return await GenerateTokensAsync(user, userAgent, ipAddress);
        }

        public async Task<TokenResponse> GoogleLoginAsync(string idToken, string? userAgent, string? ipAddress)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleSettings.ClientId }
                });

                if (payload == null)
                {
                    throw new UnauthorizedAccessException("Invalid Google token");
                }

                var user = await _userRepository.GetByEmailAsync(payload.Email);

                if (user == null)
                {
                    // Create new user if not exists
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = payload.Email,
                        FullName = payload.Name,
                        Role = UserRoleEnum.User,
                        IsEmailVerified = true, // Google already verified this
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow,
                        // Provide a random password since they use Google
                        PasswordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                        PasswordHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
                    };

                    await _userRepository.CreateAsync(user);
                }
                else
                {
                    // Update user info
                    user.LastLoginAt = DateTime.UtcNow;
                    if (!user.IsEmailVerified)
                    {
                        user.IsEmailVerified = true;
                    }
                    await _userRepository.UpdateAsync(user);
                }

                return await GenerateTokensAsync(user, userAgent, ipAddress);
            }
            catch (InvalidJwtException ex)
            {
                throw new UnauthorizedAccessException("Invalid Google token: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during Google login: " + ex.Message);
            }
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? userAgent, string? ipAddress)
        {
            // Get session by refresh token
            var session = await _sessionRepository.GetByRefreshTokenAsync(refreshToken);
            if (session == null || !session.IsActive || session.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(session.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            // Revoke old session
            await _sessionRepository.RevokeSessionAsync(session.Id);

            // Generate new tokens
            return await GenerateTokensAsync(user, userAgent, ipAddress);
        }

        public async Task LogoutAsync(Guid userId, string? refreshToken)
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var session = await _sessionRepository.GetByRefreshTokenAsync(refreshToken);
                if (session != null && session.UserId == userId)
                {
                    await _sessionRepository.RevokeSessionAsync(session.Id);
                }
            }
        }

        public async Task LogoutAllSessionsAsync(Guid userId)
        {
            await _sessionRepository.RevokeAllUserSessionsAsync(userId);
        }

        public async Task<List<SessionDto>> GetActiveSessionsAsync(Guid userId)
        {
            var sessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId);
            return sessions.Select(s => new SessionDto
            {
                Id = s.Id,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                UserAgent = s.UserAgent,
                IpAddress = s.IpAddress,
                IsActive = s.IsActive
            }).ToList();
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Verify current password
            if (!VerifyPasswordHash(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                throw new UnauthorizedAccessException("Current password is incorrect");
            }

            // Create new password hash
            CreatePasswordHash(request.NewPassword, out string passwordHash, out string passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Revoke all sessions except current (force re-login everywhere)
            await _sessionRepository.RevokeAllUserSessionsAsync(userId);

            return true;
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            
            // Don't reveal if email exists for security
            if (user == null)
            {
                return true;
            }

            // Generate password reset token
            var resetToken = GenerateSecureToken();
            var resetTokenExpiration = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiresAt = resetTokenExpiration;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Send password reset email
            await _emailService.SendPasswordResetAsync(email, user.FullName ?? "User", resetToken);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByPasswordResetTokenAsync(request.Token);
            
            if (user == null || 
                user.PasswordResetTokenExpiresAt == null || 
                user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            // Create new password hash
            CreatePasswordHash(request.NewPassword, out string passwordHash, out string passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Revoke all sessions (force re-login everywhere)
            await _sessionRepository.RevokeAllUserSessionsAsync(user.Id);

            return true;
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var user = await _userRepository.GetByEmailVerificationTokenAsync(token);
            
            if (user == null || 
                user.EmailVerificationTokenExpiresAt == null || 
                user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return true;
        }

        public async Task<bool> ResendEmailVerificationAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            
            // Don't reveal if email exists for security
            if (user == null || user.IsEmailVerified)
            {
                return true;
            }

            // Generate new verification token
            var verificationToken = GenerateSecureToken();
            var verificationTokenExpiration = DateTime.UtcNow.AddDays(7); // Token expires in 7 days

            user.EmailVerificationToken = verificationToken;
            user.EmailVerificationTokenExpiresAt = verificationTokenExpiration;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Send verification email
            await _emailService.SendEmailVerificationAsync(email, user.FullName ?? "User", verificationToken);

            return true;
        }

        #region Private Helper Methods

        private async Task<TokenResponse> GenerateTokensAsync(User user, string? userAgent, string? ipAddress)
        {
            // Generate access token
            var accessToken = GenerateAccessToken(user);
            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            // Generate refresh token
            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            // Save refresh token in database
            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiration,
                CreatedAt = DateTime.UtcNow,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                IsActive = true
            };

            await _sessionRepository.CreateAsync(session);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = accessTokenExpiration,
                TokenType = "Bearer",
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }

        private string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(user.FullName))
            {
                claims.Add(new Claim(ClaimTypes.Name, user.FullName));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            passwordSalt = Convert.ToBase64String(salt);
            passwordHash = Convert.ToBase64String(hash);
        }

        private bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            using var hmac = new HMACSHA512(saltBytes);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            var computedHashString = Convert.ToBase64String(computedHash);

            return computedHashString == storedHash;
        }

        private string GenerateSecureToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        #endregion
    }
}
