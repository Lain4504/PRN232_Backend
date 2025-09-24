namespace PRN232_Backend;

public class ReviewService
{
    
}
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using System.Threading;

namespace BookStore.Services.Service;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;

    public ReviewService(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<List<Review>> GetReviewsByBookAsync(string bookId, CancellationToken cancellationToken = default)
    {
        return await _reviewRepository.GetReviewsByBookAsync(bookId, cancellationToken);
    }

    public async Task<Review?> GetReviewByIdAsync(string reviewId, CancellationToken cancellationToken = default)
    {
        return await _reviewRepository.GetReviewByIdAsync(reviewId, cancellationToken);
    }

    public async Task AddReviewAsync(Review review, CancellationToken cancellationToken = default)
    {
        await _reviewRepository.AddReviewAsync(review, cancellationToken);
    }

    public async Task AddReplyAsync(ReviewReply reply, CancellationToken cancellationToken = default)
    {
        await _reviewRepository.AddReplyAsync(reply, cancellationToken);
    }
}
