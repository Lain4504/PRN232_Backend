using Microsoft.AspNetCore.Mvc;
using BookStore.Services.IServices;
using BookStore.Common;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;
using AutoMapper;
using FluentValidation;
using System.Net;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/authors")]
    public class AuthorController : ControllerBase
    {
        private readonly IAuthorService _authorService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateAuthorRequestDto> _createValidator;
        private readonly IValidator<UpdateAuthorRequestDto> _updateValidator;

        public AuthorController(
            IAuthorService authorService,
            IMapper mapper,
            IValidator<CreateAuthorRequestDto> createValidator,
            IValidator<UpdateAuthorRequestDto> updateValidator)
        {
            _authorService = authorService;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAuthors(CancellationToken cancellationToken)
        {
            try
            {
                var authors = await _authorService.GetAllAsync(cancellationToken);
                var authorDtos = _mapper.Map<IEnumerable<AuthorResponseDto>>(authors);

                var response = GenericResponse<IEnumerable<AuthorResponseDto>>.CreateSuccess(authorDtos, "Authors retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to retrieve authors",
                    HttpStatusCode.InternalServerError,
                    "GET_AUTHORS_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetAuthorById(long id, CancellationToken cancellationToken)
        {
            try
            {
                var author = await _authorService.GetByIdAsync(id, cancellationToken);

                if (author == null)
                {
                    var notFoundResponse = GenericResponse<object>.CreateError(
                        "Author not found",
                        HttpStatusCode.NotFound,
                        "AUTHOR_NOT_FOUND");
                    return StatusCode(notFoundResponse.StatusCode, notFoundResponse);
                }

                var authorDto = _mapper.Map<AuthorResponseDto>(author);
                var response = GenericResponse<AuthorResponseDto>.CreateSuccess(authorDto, "Author retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to retrieve author",
                    HttpStatusCode.InternalServerError,
                    "GET_AUTHOR_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAuthor([FromBody] CreateAuthorRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);

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

                var author = _mapper.Map<BookStore.Data.Model.Author>(request);
                var createdAuthor = await _authorService.CreateAuthorAsync(author, cancellationToken);
                var responseDto = _mapper.Map<AuthorResponseDto>(createdAuthor);

                var response = GenericResponse<AuthorResponseDto>.CreateSuccess(responseDto, "Author created successfully");
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to create author",
                    HttpStatusCode.InternalServerError,
                    "CREATE_AUTHOR_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateAuthor(long id, [FromBody] UpdateAuthorRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);

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

                var existingAuthor = await _authorService.GetByIdAsync(id, cancellationToken);
                if (existingAuthor == null)
                {
                    var notFoundResponse = GenericResponse<object>.CreateError(
                        "Author not found",
                        HttpStatusCode.NotFound,
                        "AUTHOR_NOT_FOUND");
                    return StatusCode(notFoundResponse.StatusCode, notFoundResponse);
                }

                _mapper.Map(request, existingAuthor);
                var updatedAuthor = await _authorService.UpdateAuthorAsync(existingAuthor, cancellationToken);
                var responseDto = _mapper.Map<AuthorResponseDto>(updatedAuthor);

                var response = GenericResponse<AuthorResponseDto>.CreateSuccess(responseDto, "Author updated successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to update author",
                    HttpStatusCode.InternalServerError,
                    "UPDATE_AUTHOR_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteAuthor(long id, CancellationToken cancellationToken)
        {
            try
            {
                var existingAuthor = await _authorService.GetByIdAsync(id, cancellationToken);
                if (existingAuthor == null)
                {
                    var notFoundResponse = GenericResponse<object>.CreateError(
                        "Author not found",
                        HttpStatusCode.NotFound,
                        "AUTHOR_NOT_FOUND");
                    return StatusCode(notFoundResponse.StatusCode, notFoundResponse);
                }

                var success = await _authorService.DeleteAuthorAsync(id, cancellationToken);
                if (!success)
                {
                    var errorResponse = GenericResponse<object>.CreateError(
                        "Failed to delete author",
                        HttpStatusCode.InternalServerError,
                        "DELETE_AUTHOR_ERROR");
                    return StatusCode(errorResponse.StatusCode, errorResponse);
                }

                var response = GenericResponse<object>.CreateSuccess(null, "Author deleted successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to delete author",
                    HttpStatusCode.InternalServerError,
                    "DELETE_AUTHOR_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }
    }
}