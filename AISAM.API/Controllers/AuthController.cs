using System.Net;
using System.Security.Claims;
using AISAM.API.DTO.Request;
using AISAM.API.DTO.Response;
using AISAM.Common;
using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Repositories;
using AISAM.Services.IServices;
using BookStore.API.DTO.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using FluentValidation;
using UserResponseDto = AISAM.Common.Models.UserResponseDto;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IValidator<CreateUserRequestDto> _validator;
        public AuthController(
            IAuthService authService, 
            IJwtService jwtService,
            IUserService userService,
            AISAMContext dbContext,
            IMapper mapper,
            IValidator<CreateUserRequestDto> validator,
            IConfiguration configuration)
        {
            _authService = authService;
            _jwtService = jwtService;
            _userService = userService;
            _mapper = mapper;
            _validator = validator;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var (success, user, message) = await _authService.LoginAsync(request.Email, request.Password, ipAddress);

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

    // API Create User với FluentValidation (email + password)
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        // Sử dụng FluentValidation để validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            
            var badRequest = GenericResponse<object>.CreateError(
                "FluentValidation failed", 
                HttpStatusCode.BadRequest, 
                "FLUENT_VALIDATION_ERROR");
            
            // Thêm FluentValidation errors vào ErrorDetails
            badRequest.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "FluentValidationErrors", errors }
            };
            
            return StatusCode(badRequest.StatusCode, badRequest);
        }

        try
        {
            // Sử dụng AutoMapper để map từ DTO sang Entity
            var user = _mapper.Map<User>(request);
            
            // Tạo user
            var createdUser = await _userService.CreateUserAsync(user, cancellationToken);
            
            // Map lại sang DTO để trả về
            var responseDto = _mapper.Map<UserResponseDto>(createdUser);
            
            var response = GenericResponse<UserResponseDto>.CreateSuccess(responseDto, "User created successfully");
            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Failed to create user", 
                HttpStatusCode.InternalServerError, 
                "CREATE_USER_ERROR");
            
            // Thêm exception message vào ErrorDetails
            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionDetails", new List<string> { ex.Message } }
            };
            
            return StatusCode(error.StatusCode, error);
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

                if (!Guid.TryParse(userId, out var parsedUserId))
                {
                    var errorResponse2 = GenericResponse<object>.CreateError(
                        "Invalid user id",
                        HttpStatusCode.BadRequest,
                        "INVALID_USER_ID");
                    return StatusCode(errorResponse2.StatusCode, errorResponse2);
                }

                var success = await _authService.LogoutAllAsync(parsedUserId);

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

                if (!Guid.TryParse(userId, out var parsedUserId))
                {
                    var errorResponse2 = GenericResponse<object>.CreateError(
                        "Invalid user id",
                        HttpStatusCode.BadRequest,
                        "INVALID_USER_ID");
                    return StatusCode(errorResponse2.StatusCode, errorResponse2);
                }

                var refreshTokens = await _authService.GetUserRefreshTokensAsync(parsedUserId);

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

                if (!Guid.TryParse(userId, out var parsedUserId))
                {
                    var errorResponse2 = GenericResponse<object>.CreateError(
                        "Invalid user id",
                        HttpStatusCode.BadRequest,
                        "INVALID_USER_ID");
                    return StatusCode(errorResponse2.StatusCode, errorResponse2);
                }

                var user = await _authService.GetUserByIdAsync(parsedUserId, cancellationToken);
                
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
