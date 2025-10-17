using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            // Example value: "v1,whsec_<base64-secret>"
            _hookSecret = Environment.GetEnvironmentVariable("SUPABASE_HOOK_SECRET")
                           ?? configuration["Supabase:HookSecret"]
                           ?? string.Empty;
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

            // Standard Webhooks headers
            var webhookId = Request.Headers["webhook-id"].FirstOrDefault();
            var webhookTimestamp = Request.Headers["webhook-timestamp"].FirstOrDefault();
            var webhookSignature = Request.Headers["webhook-signature"].FirstOrDefault();

            if (!VerifyStandardWebhook(rawBody, webhookId, webhookTimestamp, webhookSignature, _hookSecret, out var reason))
            {
                _logger.LogWarning("Invalid Supabase hook signature: {Reason}", reason ?? "unknown");
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
                await _userService.CreateUserAsync(supabaseUserGuid, email);
            }
            catch (InvalidOperationException dupEx)
            {
                _logger.LogWarning(dupEx, "User already exists for id {UserId}", supabaseUserGuid);
                return Conflict(new { error = new { http_code = 409, message = "User already exists" } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provision user failed");
                return StatusCode(500, new { error = new { http_code = 500, message = "Provision failed" } });
            }

            // Per docs, return original event on success
            return Content(rawBody, "application/json", Encoding.UTF8);
        }

        private static bool VerifyStandardWebhook(
            string payload,
            string? webhookId,
            string? webhookTimestamp,
            string? webhookSignature,
            string hookSecret,
            out string? reason)
        {
            reason = null;
            if (string.IsNullOrWhiteSpace(webhookId) || string.IsNullOrWhiteSpace(webhookTimestamp) || string.IsNullOrWhiteSpace(webhookSignature))
            {
                reason = "missing headers";
                return false;
            }

            // reject stale timestamps (tolerance 5 minutes)
            if (!long.TryParse(webhookTimestamp, out var tsSeconds))
            {
                reason = "invalid timestamp";
                return false;
            }
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(tsSeconds);
            if (DateTimeOffset.UtcNow - timestamp > TimeSpan.FromMinutes(5))
            {
                reason = "timestamp too old";
                return false;
            }

            // Extract signature value (accept formats: "v1,<sig>", "v1=<sig>", or raw base64)
            var sig = webhookSignature.Trim();
            if (sig.StartsWith("v1,", StringComparison.OrdinalIgnoreCase))
            {
                sig = sig.Substring(3);
            }
            else if (sig.StartsWith("v1=", StringComparison.OrdinalIgnoreCase))
            {
                sig = sig.Substring(3);
            }

            // Prepare signed input: id.timestamp.payload (Standard Webhooks convention)
            var signedInput = string.Concat(webhookId, ".", webhookTimestamp, ".", payload);

            // Single secret only
            var secret = hookSecret;
            if (secret.StartsWith("v1,whsec_", StringComparison.OrdinalIgnoreCase))
            {
                secret = secret.Substring("v1,whsec_".Length);
            }

            // Base64 decode secret to bytes
            byte[] keyBytes;
            try
            {
                keyBytes = Convert.FromBase64String(secret);
            }
            catch
            {
                reason = "invalid base64 secret";
                return false;
            }

            using var hmac = new HMACSHA256(keyBytes);
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedInput));
            var computedBase64 = Convert.ToBase64String(computed);

            if (CryptographicEquals(computedBase64, sig))
            {
                return true;
            }

            reason = "signature mismatch";
            return false;
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


