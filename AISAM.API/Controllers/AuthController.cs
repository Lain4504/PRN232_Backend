using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var userAgent = Request.Headers["User-Agent"].ToString();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _authService.RegisterAsync(request, userAgent, ipAddress);

                return Ok(GenericResponse<object>.CreateSuccess(
                    result,
                    "User registered successfully"
                ));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred during registration"));
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var userAgent = Request.Headers["User-Agent"].ToString();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _authService.LoginAsync(request, userAgent, ipAddress);

                return Ok(GenericResponse<object>.CreateSuccess(
                    result,
                    "Login successful"
                ));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(GenericResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred during login"));
            }
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var userAgent = Request.Headers["User-Agent"].ToString();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _authService.GoogleLoginAsync(request.IdToken, userAgent, ipAddress);

                return Ok(GenericResponse<object>.CreateSuccess(
                    result,
                    "Google login successful"
                ));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(GenericResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred during Google login"));
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userAgent = Request.Headers["User-Agent"].ToString();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _authService.RefreshTokenAsync(request.RefreshToken, userAgent, ipAddress);

                return Ok(GenericResponse<object>.CreateSuccess(
                    result,
                    "Token refreshed successfully"
                ));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(GenericResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred during token refresh"));
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _authService.LogoutAsync(userId, request?.RefreshToken);

                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "Logout successful"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred during logout"));
            }
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAllSessions()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _authService.LogoutAllSessionsAsync(userId);

                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "All sessions logged out successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout all sessions");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred during logout"));
            }
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var sessions = await _authService.GetActiveSessionsAsync(userId);

                return Ok(GenericResponse<object>.CreateSuccess(
                    sessions,
                    "Active sessions retrieved successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active sessions");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred retrieving sessions"));
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _authService.ChangePasswordAsync(userId, request);

                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "Password changed successfully. Please login again."
                ));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(GenericResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred changing password"));
            }
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email);
                var role = User.FindFirstValue(ClaimTypes.Role);
                var name = User.FindFirstValue(ClaimTypes.Name);

                var userData = new
                {
                    id = userId,
                    email = email,
                    fullName = name,
                    role = role
                };

                return Ok(GenericResponse<object>.CreateSuccess(
                    userData,
                    "Current user retrieved successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred"));
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _authService.ForgotPasswordAsync(request.Email);

                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "If the email exists, a password reset link has been sent"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password");
                // Don't reveal if email exists or not for security
                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "If the email exists, a password reset link has been sent"
                ));
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(request);

                if (!result)
                {
                    return BadRequest(GenericResponse<object>.CreateError("Invalid or expired reset token"));
                }

                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "Password reset successfully. Please login with your new password."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred during password reset"));
            }
        }

        [HttpPost("change-password-with-token")]
        public async Task<IActionResult> ChangePasswordWithToken([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(request);

                if (!result)
                {
                    return BadRequest(GenericResponse<object>.CreateError("Invalid or expired reset token"));
                }

                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "Password changed successfully. Please login with your new password."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred during password change"));
            }
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                var result = await _authService.VerifyEmailAsync(token);

                if (!result)
                {
                    return BadRequest(GenericResponse<object>.CreateError("Invalid or expired verification token"));
                }

                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "Email verified successfully. You can now login."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification");
                return StatusCode(500, GenericResponse<object>.CreateError("An error occurred during email verification"));
            }
        }

        [HttpPost("verify-email/resend")]
        public async Task<IActionResult> ResendEmailVerification([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _authService.ResendEmailVerificationAsync(request.Email);

                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "If the email exists and is not verified, a verification email has been sent"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email");
                // Don't reveal if email exists or not for security
                return Ok(GenericResponse<object>.CreateSuccess(
                    null,
                    "If the email exists and is not verified, a verification email has been sent"
                ));
            }
        }
    }
}
