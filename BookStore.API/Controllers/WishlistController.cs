using Microsoft.AspNetCore.Mvc;
using BookStore.Services;
using BookStore.Common;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;
using AutoMapper;
using FluentValidation;
using System.Net;
using System.Threading;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/wishlist")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateWishlistRequestDto> _validator;

    public WishlistController(
        IWishlistService wishlistService,
        IMapper mapper,
        IValidator<CreateWishlistRequestDto> validator)
    {
        _wishlistService = wishlistService;
        _mapper = mapper;
        _validator = validator;
    }

    // Thêm wishlist mới với FluentValidation
    [HttpPost]
    public async Task<IActionResult> AddWishlist([FromBody] CreateWishlistRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            var badRequest = GenericResponse<object>.CreateError(
                "Validation failed",
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR");

            badRequest.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ValidationErrors", errors }
            };

            return StatusCode(badRequest.StatusCode, badRequest);
        }

        try
        {
            var wishlist = _mapper.Map<BookStore.Data.Model.Wishlist>(request);

            await _wishlistService.AddWishlistAsync(wishlist, cancellationToken);

            var response = GenericResponse<object>.CreateSuccess(new { }, "Wishlist added successfully");
            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Failed to add wishlist",
                HttpStatusCode.InternalServerError,
                "ADD_WISHLIST_ERROR");

            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionDetails", new List<string> { ex.Message } }
            };

            return StatusCode(error.StatusCode, error);
        }
    }

    // Lấy danh sách wishlist theo userId
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetWishlistsByUser([FromRoute] string userId, CancellationToken cancellationToken)
    {
        try
        {
            var wishlists = await _wishlistService.GetWishlistByUserAsync(userId, cancellationToken);

            if (wishlists == null || !wishlists.Any())
            {
                return NoContent();
            }

            var responseDtos = _mapper.Map<List<WishlistResponseDto>>(wishlists);

            var response = GenericResponse<List<WishlistResponseDto>>.CreateSuccess(responseDtos);
            return Ok(response);
        }
        catch (Exception)
        {
            var error = GenericResponse<object>.CreateError(
                "Internal server error. Please try again later.",
                HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR");

            return StatusCode(error.StatusCode, error);
        }
    }

    // Xóa wishlist theo Id
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWishlist([FromRoute] long id, CancellationToken cancellationToken)
    {
        try
        {
            await _wishlistService.DeleteWishlistAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Failed to delete wishlist",
                HttpStatusCode.InternalServerError,
                "DELETE_WISHLIST_ERROR");

            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionDetails", new List<string> { ex.Message } }
            };

            return StatusCode(error.StatusCode, error);
        }
    }

    // Xóa toàn bộ wishlist theo userId
    [HttpDelete("all-{userId}")]
    public async Task<IActionResult> DeleteAllWishlists([FromRoute] string userId, CancellationToken cancellationToken)
    {
        try
        {
            await _wishlistService.DeleteAllWishlistAsync(userId, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            var error = GenericResponse<object>.CreateError(
                "Failed to delete all wishlists",
                HttpStatusCode.InternalServerError,
                "DELETE_ALL_WISHLIST_ERROR");

            error.Error.ValidationErrors = new Dictionary<string, List<string>>
            {
                { "ExceptionDetails", new List<string> { ex.Message } }
            };

            return StatusCode(error.StatusCode, error);
        }
    }
}
