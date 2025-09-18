using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BookStore.Common;
using BookStore.Services.IServices;
using BookStore.Services.Service;
using BookStore.Common.Models;
using System.Security.Claims;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IJwtService jwtService, ILogger<UserController> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<GenericResponse<LoginResponseDto>>> Register([FromBody] RegisterUserDto registerDto)
        {
            try
            {
                _logger.LogInformation("User registration attempt for email: {Email}", registerDto.Email);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                        );

                    return BadRequest(GenericResponse<LoginResponseDto>.CreateError(
                        "Dữ liệu không hợp lệ",
                        System.Net.HttpStatusCode.BadRequest,
                        "VALIDATION_ERROR"
                    ));
                }

                var user = await _userService.RegisterUserAsync(registerDto.Email, registerDto.Username, registerDto.Password);
                var token = _jwtService.GenerateToken(user);

                var response = new LoginResponseDto
                {
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    User = new UserResponseDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? "",
                        Username = user.Username ?? "",
                        CreatedAt = user.CreatedAt,
                        SocialAccounts = user.SocialAccounts?.Select(sa => new SocialAccountDto
                        {
                            Id = sa.Id,
                            Provider = sa.Provider,
                            ProviderUserId = sa.ProviderUserId,
                            CreatedAt = sa.CreatedAt,
                            Targets = sa.Targets?.Select(st => new SocialTargetDto
                            {
                                Id = st.Id,
                                ProviderTargetId = st.ProviderTargetId,
                                Name = st.Name,
                                Type = st.Type
                            }).ToList() ?? new List<SocialTargetDto>()
                        }).ToList() ?? new List<SocialAccountDto>()
                    }
                };

                _logger.LogInformation("User registered successfully: {UserId}", user.Id);
                return Ok(GenericResponse<LoginResponseDto>.CreateSuccess(response, "Đăng ký thành công"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Registration failed: {Message}", ex.Message);
                return BadRequest(GenericResponse<LoginResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, GenericResponse<LoginResponseDto>.CreateError(
                    "Đã xảy ra lỗi trong quá trình đăng ký",
                    System.Net.HttpStatusCode.InternalServerError,
                    "REGISTRATION_ERROR"
                ));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<GenericResponse<LoginResponseDto>>> Login([FromBody] LoginUserDto loginDto)
        {
            try
            {
                _logger.LogInformation("User login attempt for: {EmailOrUsername}", loginDto.EmailOrUsername);

                if (!ModelState.IsValid)
                {
                    return BadRequest(GenericResponse<LoginResponseDto>.CreateError("Dữ liệu không hợp lệ"));
                }

                var user = await _userService.LoginUserAsync(loginDto.EmailOrUsername, loginDto.Password);
                if (user == null)
                {
                    _logger.LogWarning("Login failed for: {EmailOrUsername}", loginDto.EmailOrUsername);
                    return Unauthorized(GenericResponse<LoginResponseDto>.CreateError(
                        "Email/Username hoặc mật khẩu không đúng",
                        System.Net.HttpStatusCode.Unauthorized,
                        "INVALID_CREDENTIALS"
                    ));
                }

                var token = _jwtService.GenerateToken(user);

                var response = new LoginResponseDto
                {
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    User = new UserResponseDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? "",
                        Username = user.Username ?? "",
                        CreatedAt = user.CreatedAt,
                        SocialAccounts = user.SocialAccounts?.Select(sa => new SocialAccountDto
                        {
                            Id = sa.Id,
                            Provider = sa.Provider,
                            ProviderUserId = sa.ProviderUserId,
                            CreatedAt = sa.CreatedAt,
                            Targets = sa.Targets?.Select(st => new SocialTargetDto
                            {
                                Id = st.Id,
                                ProviderTargetId = st.ProviderTargetId,
                                Name = st.Name,
                                Type = st.Type
                            }).ToList() ?? new List<SocialTargetDto>()
                        }).ToList() ?? new List<SocialAccountDto>()
                    }
                };

                _logger.LogInformation("User logged in successfully: {UserId}", user.Id);
                return Ok(GenericResponse<LoginResponseDto>.CreateSuccess(response, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, GenericResponse<LoginResponseDto>.CreateError(
                    "Đã xảy ra lỗi trong quá trình đăng nhập",
                    System.Net.HttpStatusCode.InternalServerError,
                    "LOGIN_ERROR"
                ));
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<UserResponseDto>>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(GenericResponse<UserResponseDto>.CreateError("Token không hợp lệ"));
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(GenericResponse<UserResponseDto>.CreateError("Không tìm thấy người dùng"));
                }

                var response = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    Username = user.Username ?? "",
                    CreatedAt = user.CreatedAt,
                    SocialAccounts = user.SocialAccounts?.Select(sa => new SocialAccountDto
                    {
                        Id = sa.Id,
                        Provider = sa.Provider,
                        ProviderUserId = sa.ProviderUserId,
                        CreatedAt = sa.CreatedAt,
                        Targets = sa.Targets?.Select(st => new SocialTargetDto
                        {
                            Id = st.Id,
                            ProviderTargetId = st.ProviderTargetId,
                            Name = st.Name,
                            Type = st.Type
                        }).ToList() ?? new List<SocialTargetDto>()
                    }).ToList() ?? new List<SocialAccountDto>()
                };

                return Ok(GenericResponse<UserResponseDto>.CreateSuccess(response, "Lấy thông tin thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, GenericResponse<UserResponseDto>.CreateError(
                    "Đã xảy ra lỗi khi lấy thông tin người dùng"
                ));
            }
        }

        [HttpGet("social-accounts")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<List<SocialAccountDto>>>> GetSocialAccounts()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(GenericResponse<List<SocialAccountDto>>.CreateError("Token không hợp lệ"));
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(GenericResponse<List<SocialAccountDto>>.CreateError("Không tìm thấy người dùng"));
                }

                var socialAccounts = user.SocialAccounts?.Select(sa => new SocialAccountDto
                {
                    Id = sa.Id,
                    Provider = sa.Provider,
                    ProviderUserId = sa.ProviderUserId,
                    CreatedAt = sa.CreatedAt,
                    Targets = sa.Targets?.Select(st => new SocialTargetDto
                    {
                        Id = st.Id,
                        ProviderTargetId = st.ProviderTargetId,
                        Name = st.Name,
                        Type = st.Type
                    }).ToList() ?? new List<SocialTargetDto>()
                }).ToList() ?? new List<SocialAccountDto>();

                return Ok(GenericResponse<List<SocialAccountDto>>.CreateSuccess(
                    socialAccounts, 
                    "Lấy danh sách tài khoản mạng xã hội thành công"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting social accounts");
                return StatusCode(500, GenericResponse<List<SocialAccountDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy danh sách tài khoản mạng xã hội"
                ));
            }
        }
    }
}
