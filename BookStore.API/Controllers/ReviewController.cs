namespace PRN232_Backend;

public class ReviewController
{
    
}
using BookStore.Data.Model;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;
using BookStore.Services.Service;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BookStore.API.Controllers;

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
        // TODO: Gán UserId từ token nếu có xác thực
        await _reviewService.AddReviewAsync(review, cancellationToken);
        return StatusCode(201, GenericResponse<object>.CreateSuccess(new { }, "Review added successfully"));
    }

    // Lấy danh sách review theo bookId
    [HttpGet("book/{bookId}")]
    public async Task<IActionResult> GetReviewsByBook([FromRoute] string bookId, CancellationToken cancellationToken)
    {
        var reviews = await _reviewService.GetReviewsByBookAsync(bookId, cancellationToken);
        var responseDtos = _mapper.Map<List<ReviewResponseDto>>(reviews);
        return Ok(GenericResponse<List<ReviewResponseDto>>.CreateSuccess(responseDtos));
    }

    // Thêm phản hồi cho review
    [HttpPost("reply")]
    public async Task<IActionResult> AddReply([FromBody] CreateReviewReplyRequestDto request, CancellationToken cancellationToken)
    {
        var reply = _mapper.Map<ReviewReply>(request);
        // TODO: Gán UserId từ token nếu có xác thực
        await _reviewService.AddReplyAsync(reply, cancellationToken);
        return StatusCode(201, GenericResponse<object>.CreateSuccess(new { }, "Reply added successfully"));
    }
}
