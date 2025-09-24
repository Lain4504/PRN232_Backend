namespace PRN232_Backend;

public class ReviewRepository
{
    
}
using BookStore.Data;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace BookStore.Repositories.Repository;

public class ReviewRepository : IReviewRepository
{
    private readonly BookStoreDbContext _context;

    public ReviewRepository(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<List<Review>> GetReviewsByBookAsync(string bookId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Book)
            .Include(r => r.Replies)
            .Where(r => r.BookId == bookId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Review?> GetReviewByIdAsync(string reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Book)
            .Include(r => r.Replies)
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
    }

    public async Task AddReviewAsync(Review review, CancellationToken cancellationToken = default)
    {
        await _context.Reviews.AddAsync(review, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddReplyAsync(ReviewReply reply, CancellationToken cancellationToken = default)
    {
        await _context.ReviewReplies.AddAsync(reply, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
