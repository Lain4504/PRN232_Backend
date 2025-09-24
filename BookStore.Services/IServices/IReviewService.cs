using BookStore.Data.Model;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Services
{
    public interface IReviewService
    {
        Task<List<Review>> GetReviewsByBookAsync(long bookId, CancellationToken cancellationToken = default);
        Task<Review?> GetReviewByIdAsync(string reviewId, CancellationToken cancellationToken = default);
        Task AddReviewAsync(Review review, CancellationToken cancellationToken = default);
        Task AddReplyAsync(ReviewReply reply, CancellationToken cancellationToken = default);
    }
}
