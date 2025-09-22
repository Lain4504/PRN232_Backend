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
    [Route("api/publishers")]
    public class PublisherController : ControllerBase
    {
        private readonly IPublisherService _publisherService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreatePublisherRequestDto> _createValidator;
        private readonly IValidator<UpdatePublisherRequestDto> _updateValidator;

        public PublisherController(
            IPublisherService publisherService,
            IMapper mapper,
            IValidator<CreatePublisherRequestDto> createValidator,
            IValidator<UpdatePublisherRequestDto> updateValidator)
        {
            _publisherService = publisherService;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPublishers(CancellationToken cancellationToken)
        {
            try
            {
                var publishers = await _publisherService.GetAllAsync(cancellationToken);
                var publisherDtos = _mapper.Map<IEnumerable<PublisherResponseDto>>(publishers);

                var response = GenericResponse<IEnumerable<PublisherResponseDto>>.CreateSuccess(publisherDtos, "Publishers retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to retrieve publishers",
                    HttpStatusCode.InternalServerError,
                    "GET_PUBLISHERS_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetPublisherById(long id, CancellationToken cancellationToken)
        {
            try
            {
                var publisher = await _publisherService.GetByIdAsync(id, cancellationToken);

                if (publisher == null)
                {
                    var notFoundResponse = GenericResponse<object>.CreateError(
                        "Publisher not found",
                        HttpStatusCode.NotFound,
                        "PUBLISHER_NOT_FOUND");
                    return StatusCode(notFoundResponse.StatusCode, notFoundResponse);
                }

                var publisherDto = _mapper.Map<PublisherResponseDto>(publisher);
                var response = GenericResponse<PublisherResponseDto>.CreateSuccess(publisherDto, "Publisher retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to retrieve publisher",
                    HttpStatusCode.InternalServerError,
                    "GET_PUBLISHER_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePublisher([FromBody] CreatePublisherRequestDto request, CancellationToken cancellationToken)
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

                var publisher = _mapper.Map<BookStore.Data.Model.Publisher>(request);
                var createdPublisher = await _publisherService.CreatePublisherAsync(publisher, cancellationToken);
                var responseDto = _mapper.Map<PublisherResponseDto>(createdPublisher);

                var response = GenericResponse<PublisherResponseDto>.CreateSuccess(responseDto, "Publisher created successfully");
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to create publisher",
                    HttpStatusCode.InternalServerError,
                    "CREATE_PUBLISHER_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdatePublisher(long id, [FromBody] UpdatePublisherRequestDto request, CancellationToken cancellationToken)
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

                var existingPublisher = await _publisherService.GetByIdAsync(id, cancellationToken);
                if (existingPublisher == null)
                {
                    var notFoundResponse = GenericResponse<object>.CreateError(
                        "Publisher not found",
                        HttpStatusCode.NotFound,
                        "PUBLISHER_NOT_FOUND");
                    return StatusCode(notFoundResponse.StatusCode, notFoundResponse);
                }

                _mapper.Map(request, existingPublisher);
                var updatedPublisher = await _publisherService.UpdatePublisherAsync(existingPublisher, cancellationToken);
                var responseDto = _mapper.Map<PublisherResponseDto>(updatedPublisher);

                var response = GenericResponse<PublisherResponseDto>.CreateSuccess(responseDto, "Publisher updated successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to update publisher",
                    HttpStatusCode.InternalServerError,
                    "UPDATE_PUBLISHER_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeletePublisher(long id, CancellationToken cancellationToken)
        {
            try
            {
                var existingPublisher = await _publisherService.GetByIdAsync(id, cancellationToken);
                if (existingPublisher == null)
                {
                    var notFoundResponse = GenericResponse<object>.CreateError(
                        "Publisher not found",
                        HttpStatusCode.NotFound,
                        "PUBLISHER_NOT_FOUND");
                    return StatusCode(notFoundResponse.StatusCode, notFoundResponse);
                }

                var success = await _publisherService.DeletePublisherAsync(id, cancellationToken);
                if (!success)
                {
                    var errorResponse = GenericResponse<object>.CreateError(
                        "Failed to delete publisher",
                        HttpStatusCode.InternalServerError,
                        "DELETE_PUBLISHER_ERROR");
                    return StatusCode(errorResponse.StatusCode, errorResponse);
                }

                var response = GenericResponse<object>.CreateSuccess(null, "Publisher deleted successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = GenericResponse<object>.CreateError(
                    "Failed to delete publisher",
                    HttpStatusCode.InternalServerError,
                    "DELETE_PUBLISHER_ERROR");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
        }
    }
}