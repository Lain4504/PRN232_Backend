using Microsoft.AspNetCore.Mvc;
using BookStore.Services;
using BookStore.Common;
using BookStore.API.DTO.Response;
using BookStore.API.DTO.Request;
using AutoMapper;
using System.Net;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;

    public UserController(IUserService userService, IMapper mapper)
    {
        _userService = userService;
        _mapper = mapper;
    }

    // API 1: Manual mapping - trả về thiếu field (không có PhoneNumber và Address)
    [HttpGet("{id}/manual")]
    public async Task<IActionResult> GetByIdManual([FromRoute] string id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            var notFound = GenericResponse<UserResponseDto>.CreateError("User not found", HttpStatusCode.NotFound, "USER_NOT_FOUND");
            return StatusCode(notFound.StatusCode, notFound);
        }

        // Manual mapping - thiếu PhoneNumber và Address
        var dto = new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber, // Thiếu field này
            Address = user.Address, // Thiếu field này
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        var response = GenericResponse<UserResponseDto>.CreateSuccess(dto);
        return Ok(response);
    }

    // API 2: AutoMapper - trả về đầy đủ fields
    [HttpGet("{id}/automapper")]
    public async Task<IActionResult> GetByIdAutoMapper([FromRoute] string id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            var notFound = GenericResponse<UserResponseDto>.CreateError("User not found", HttpStatusCode.NotFound, "USER_NOT_FOUND");
            return StatusCode(notFound.StatusCode, notFound);
        }

        // AutoMapper mapping - đầy đủ fields
        var dto = _mapper.Map<UserResponseDto>(user);

        var response = GenericResponse<UserResponseDto>.CreateSuccess(dto);
        return Ok(response);
    }

    // API 3: AutoMapper với chỉ định fields cụ thể (sử dụng DTO riêng cho selective mapping)
    [HttpGet("{id}/automapper-selective")]
    public async Task<IActionResult> GetByIdAutoMapperSelective([FromRoute] string id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            var notFound = GenericResponse<UserSelectiveResponseDto>.CreateError("User not found", HttpStatusCode.NotFound, "USER_NOT_FOUND");
            return StatusCode(notFound.StatusCode, notFound);
        }

        // AutoMapper mapping với selective fields - chỉ map Id, Username, Email, Role, Status
        // Sử dụng UserSelectiveResponseDto để chỉ trả về các fields cần thiết
        var dto = _mapper.Map<UserSelectiveResponseDto>(user);

        var response = GenericResponse<UserSelectiveResponseDto>.CreateSuccess(dto);
        return Ok(response);
    }

    // API Create User với validation
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        // Validation sẽ được thực hiện tự động bởi ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            var badRequest = GenericResponse<object>.CreateError(
                "Validation failed", 
                HttpStatusCode.BadRequest, 
                "VALIDATION_ERROR");
            
            // Thêm validation errors vào ErrorDetails
            badRequest.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ValidationErrors", errors }
            };
            
            return StatusCode(badRequest.StatusCode, badRequest);
        }

        try
        {
            // Sử dụng AutoMapper để map từ DTO sang Entity
            var user = _mapper.Map<BookStore.Data.Model.User>(request);
            
            // Tạo user
            var createdUser = await _userService.CreateUserAsync(user, cancellationToken);
            
            // Map lại sang DTO để trả về
            var responseDto = _mapper.Map<UserResponseDto>(createdUser);
            
            var response = GenericResponse<UserResponseDto>.CreateSuccess(responseDto);
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
}