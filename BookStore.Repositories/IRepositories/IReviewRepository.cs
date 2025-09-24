using BookStore.Data.Model;
using System.Threading;

namespace BookStore.Repositories.IRepositories
{
    public interface IReviewRepository
    {
        Task<List<Review>> GetReviewsByBookAsync(string bookId, CancellationToken cancellationToken = default);
        Task<Review?> GetReviewByIdAsync(string reviewId, CancellationToken cancellationToken = default);
        Task AddReviewAsync(Review review, CancellationToken cancellationToken = default);
        Task AddReplyAsync(ReviewReply reply, CancellationToken cancellationToken = default);
    }
}
