using BookStore.Data;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace BookStore.Repositories.Repository;

public class WishlistRepository : IWishlistRepository
{
	private readonly BookStoreDbContext _context;

	public WishlistRepository(BookStoreDbContext context)
	{
		_context = context;
	}

	public async Task<List<Wishlist>> GetWishlistByUserAsync(string userId, CancellationToken cancellationToken = default)
	{
		return await _context.Wishlists
			.Where(w => w.UserId == userId)
			.ToListAsync(cancellationToken);
	}

	public async Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
	{
		await _context.Wishlists.AddAsync(wishlist, cancellationToken);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task DeleteWishlistAsync(long bookId, CancellationToken cancellationToken = default)
	{
		var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.Book != null && w.Book.Id == bookId.ToString(), cancellationToken);
		if (wishlist != null)
		{
			_context.Wishlists.Remove(wishlist);
			await _context.SaveChangesAsync(cancellationToken);
		}
	}

	public async Task DeleteAllWishlistAsync(string userId, CancellationToken cancellationToken = default)
	{
		var wishlists = _context.Wishlists.Where(w => w.UserId == userId);
		_context.Wishlists.RemoveRange(wishlists);
		await _context.SaveChangesAsync(cancellationToken);
	}
}
