using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using BookStore.Services;
using System.Threading;

namespace BookStore.Services.Service;

public class WishlistService : IWishlistService
{
	private readonly IWishlistRepository _wishlistRepository;

	public WishlistService(IWishlistRepository wishlistRepository)
	{
		_wishlistRepository = wishlistRepository;
	}

	public async Task<List<Wishlist>> GetWishlistByUserAsync(string userId, CancellationToken cancellationToken = default)
	{
		return await _wishlistRepository.GetWishlistByUserAsync(userId, cancellationToken);
	}

	public async Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
	{
		await _wishlistRepository.AddWishlistAsync(wishlist, cancellationToken);
	}

	public async Task DeleteWishlistAsync(long id, CancellationToken cancellationToken = default)
	{
		await _wishlistRepository.DeleteWishlistAsync(id, cancellationToken);
	}

	public async Task DeleteAllWishlistAsync(string userId, CancellationToken cancellationToken = default)
	{
		await _wishlistRepository.DeleteAllWishlistAsync(userId, cancellationToken);
	}
}
