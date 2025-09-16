using Microsoft.AspNetCore.Mvc;
using BookStore.Services;
using BookStore.Common;
using BookStore.API.DTO.Response;
using System.Net;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            var notFound = GenericResponse<UserResponseDto>.CreateError("User not found", HttpStatusCode.NotFound, "USER_NOT_FOUND");
            return StatusCode(notFound.StatusCode, notFound);
        }

        var dto = new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        var response = GenericResponse<UserResponseDto>.CreateSuccess(dto);
        return Ok(response);
    }
}