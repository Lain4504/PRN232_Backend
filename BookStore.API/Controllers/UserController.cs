using Microsoft.AspNetCore.Mvc;
using BookStore.Services;
using BookStore.Common;
using BookStore.API.DTO.Response;
using BookStore.API.DTO.Request;
using BookStore.API.Validators;
using AutoMapper;
using FluentValidation;
using System.Net;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateUserRequestDto> _validator;

    public UserController(IUserService userService, IMapper mapper, IValidator<CreateUserRequestDto> validator)
    {
        _userService = userService;
        _mapper = mapper;
        _validator = validator;
    }

    // AutoMapper - trả về đầy đủ fields
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
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

    // API Create User với FluentValidation (email + password)
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
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
            var user = _mapper.Map<BookStore.Data.Model.User>(request);
            
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
}