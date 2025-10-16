using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("hooks/supabase/auth")] // no auth; secured via HMAC signature
    public class AuthHookController : ControllerBase
    {
        private readonly ILogger<AuthHookController> _logger;
        private readonly IUserService _userService;
        private readonly string _hookSecret;

        public AuthHookController(ILogger<AuthHookController> logger, IUserService userService, IConfiguration configuration)
        {
            _logger = logger;
            _userService = userService;
            _hookSecret = configuration["Supabase:HookSecret"] ?? Environment.GetEnvironmentVariable("SUPABASE_HOOK_SECRET") ?? string.Empty;
        }

        [HttpPost("before-user-created")]
        public async Task<IActionResult> BeforeUserCreated()
        {
            if (string.IsNullOrWhiteSpace(_hookSecret))
            {
                _logger.LogError("Supabase hook secret not configured");
                return StatusCode(500, new { error = new { http_code = 500, message = "Hook secret not configured" } });
            }

            // Read raw body
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync();

            // Verify HMAC signature (header name per Supabase: X-Supabase-Signature)
            var signatureHeader = Request.Headers["X-Supabase-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signatureHeader) || !VerifySignature(rawBody, signatureHeader, _hookSecret))
            {
                _logger.LogWarning("Invalid Supabase hook signature");
                return Unauthorized(new { error = new { http_code = 401, message = "Invalid signature" } });
            }

            SupabaseBeforeUserCreatedEvent? hookEvent;
            try
            {
                hookEvent = JsonSerializer.Deserialize<SupabaseBeforeUserCreatedEvent>(rawBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Supabase hook payload");
                return BadRequest(new { error = new { http_code = 400, message = "Invalid payload" } });
            }

            var supabaseId = hookEvent?.User?.Id;
            var email = hookEvent?.User?.Email;

            if (string.IsNullOrWhiteSpace(supabaseId) || string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { error = new { http_code = 400, message = "Missing user.id or user.email" } });
            }

            if (!Guid.TryParse(supabaseId, out var supabaseUserGuid))
            {
                return BadRequest(new { error = new { http_code = 400, message = "Invalid user.id format" } });
            }

            try
            {
                await _userService.GetOrCreateUserAsync(supabaseUserGuid, email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provision user failed");
                return StatusCode(500, new { error = new { http_code = 500, message = "Provision failed" } });
            }

            // Per docs, return original event on success
            return Content(rawBody, "application/json", Encoding.UTF8);
        }

        private static bool VerifySignature(string payload, string receivedSignature, string secret)
        {
            // signature is expected as hex of HMACSHA256
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expected = Convert.ToHexString(hash).ToLowerInvariant();
            return CryptographicEquals(expected, receivedSignature.Trim().ToLowerInvariant());
        }

        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }
    }
}


