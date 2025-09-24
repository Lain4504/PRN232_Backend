
using BookStore.Data.Model;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;
using BookStore.Services;
using BookStore.Common;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/review")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly IMapper _mapper;

        public ReviewController(IReviewService reviewService, IMapper mapper)
        {
            _reviewService = reviewService;
            _mapper = mapper;
        }

        // Tạo review mới cho sách
        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewRequestDto request, CancellationToken cancellationToken)
        {
            var review = _mapper.Map<Review>(request);
            await _reviewService.AddReviewAsync(review, cancellationToken);
            var response = GenericResponse<object>.CreateSuccess(new { }, "Review added successfully");
            return StatusCode(201, response);
        }

        // Lấy danh sách review theo bookId
        [HttpGet("book/{bookId}")]
        public async Task<IActionResult> GetReviewsByBook([FromRoute] long bookId, CancellationToken cancellationToken)
        {
            var reviews = await _reviewService.GetReviewsByBookAsync(bookId, cancellationToken);
            var responseDtos = _mapper.Map<List<ReviewResponseDto>>(reviews);
            var response = GenericResponse<List<ReviewResponseDto>>.CreateSuccess(responseDtos);
            return Ok(response);
        }

        // Thêm phản hồi cho review
        [HttpPost("reply")]
        public async Task<IActionResult> AddReply([FromBody] CreateReviewReplyRequestDto request, CancellationToken cancellationToken)
        {
            var reply = _mapper.Map<ReviewReply>(request);
            await _reviewService.AddReplyAsync(reply, cancellationToken);
            var response = GenericResponse<object>.CreateSuccess(new { }, "Reply added successfully");
            return StatusCode(201, response);
        }
    }
}
