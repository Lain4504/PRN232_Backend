using Microsoft.AspNetCore.Mvc;
using BookStore.Services.IServices;
using BookStore.Common;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;
using BookStore.Data.Model;
using AutoMapper;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService, 
            IJwtService jwtService, 
            IMapper mapper,
            IConfiguration configuration)
        {
            _authService = authService;
            _jwtService = jwtService;
            _mapper = mapper;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var (success, user, message) = await _authService.LoginAsync(request.EmailOrUsername, request.Password, ipAddress);

                if (!success || user == null)
                {
                    var errorResponse = GenericResponse<object>.CreateError(
                        message, 
                        HttpStatusCode.Unauthorized, 
                        "LOGIN_FAILED");
                    return StatusCode(errorResponse.StatusCode, errorResponse);
                }

                // Generate tokens
                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save refresh token to database
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7"))
                };

                await _authService.SaveRefreshTokenAsync(refreshTokenEntity);
                var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15"));

                var responseDto = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = _mapper.Map<UserResponseDto>(user)
                };

                var response = GenericResponse<AuthResponseDto>.CreateSuccess(responseDto, "Login successful");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Login failed", 
                    HttpStatusCode.InternalServerError, 
                    "LOGIN_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var (success, accessToken, refreshToken, message) = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

                if (!success)
                {
                    var errorResponse = GenericResponse<object>.CreateError(
                        message, 
                        HttpStatusCode.Unauthorized, 
                        "REFRESH_TOKEN_FAILED");
                    return StatusCode(errorResponse.StatusCode, errorResponse);
                }

                var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15"));

                var responseDto = new TokenResponseDto
                {
                    AccessToken = accessToken!,
                    RefreshToken = refreshToken!,
                    ExpiresAt = expiresAt
                };

                var response = GenericResponse<TokenResponseDto>.CreateSuccess(responseDto, "Token refreshed successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Token refresh failed", 
                    HttpStatusCode.InternalServerError, 
                    "REFRESH_TOKEN_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var success = await _authService.LogoutAsync(request.RefreshToken, ipAddress);

                if (!success)
                {
                    var errorResponse = GenericResponse<object>.CreateError(
                        "Invalid refresh token", 
                        HttpStatusCode.BadRequest, 
                        "LOGOUT_FAILED");
                    return StatusCode(errorResponse.StatusCode, errorResponse);
                }

                var response = GenericResponse<object>.CreateSuccess(null, "Logout successful");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Logout failed", 
                    HttpStatusCode.InternalServerError, 
                    "LOGOUT_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    var errorResponse = GenericResponse<object>.CreateError(
                        "User not found", 
                        HttpStatusCode.Unauthorized, 
                        "USER_NOT_FOUND");
                    return StatusCode(errorResponse.StatusCode, errorResponse);
                }

                var success = await _authService.LogoutAllAsync(userId);

                var response = GenericResponse<object>.CreateSuccess(null, "Logged out from all devices successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Logout all failed", 
                    HttpStatusCode.InternalServerError, 
                    "LOGOUT_ALL_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                var success = await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress, "User requested token revocation");

                if (!success)
                {
                    var errorResponse = GenericResponse<object>.CreateError(
                        "Token revocation failed", 
                        HttpStatusCode.BadRequest, 
                        "REVOKE_TOKEN_FAILED");
                    return StatusCode(errorResponse.StatusCode, errorResponse);
                }

                var response = GenericResponse<object>.CreateSuccess(null, "Token revoked successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Token revocation failed", 
                    HttpStatusCode.InternalServerError, 
                    "REVOKE_TOKEN_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpGet("refresh-tokens")]
        [Authorize]
        public async Task<IActionResult> GetRefreshTokens(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    var errorResponse = GenericResponse<object>.CreateError(
                        "User not found", 
                        HttpStatusCode.Unauthorized, 
                        "USER_NOT_FOUND");
                    return StatusCode(errorResponse.StatusCode, errorResponse);
                }

                var refreshTokens = await _authService.GetUserRefreshTokensAsync(userId);

                var response = GenericResponse<IEnumerable<RefreshToken>>.CreateSuccess(refreshTokens, "Refresh tokens retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to retrieve refresh tokens", 
                    HttpStatusCode.InternalServerError, 
                    "GET_REFRESH_TOKENS_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    var errorResponse = GenericResponse<object>.CreateError(
                        "User not found", 
                        HttpStatusCode.Unauthorized, 
                        "USER_NOT_FOUND");
                    return StatusCode(errorResponse.StatusCode, errorResponse);
                }

                var user = await _authService.GetUserByIdAsync(userId, cancellationToken);
                
                if (user == null)
                {
                    var notFoundResponse = GenericResponse<object>.CreateError(
                        "User not found", 
                        HttpStatusCode.NotFound, 
                        "USER_NOT_FOUND");
                    return StatusCode(notFoundResponse.StatusCode, notFoundResponse);
                }

                var userDto = _mapper.Map<UserResponseDto>(user);
                var response = GenericResponse<UserResponseDto>.CreateSuccess(userDto, "User information retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to retrieve user information", 
                    HttpStatusCode.InternalServerError, 
                    "GET_USER_INFO_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        private string GetIpAddress()
        {
            // Check for forwarded IP first (in case of proxy/load balancer)
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for real IP
            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fallback to connection remote IP
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
