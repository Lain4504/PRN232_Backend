
using BookStore.Data.Model;
using System.Threading;
namespace BookStore.Repositories.IRepositories;

public interface IWishlistRepository
{
	Task<List<Wishlist>> GetWishlistByUserAsync(string userId, CancellationToken cancellationToken = default);
	Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default);
	Task DeleteWishlistAsync(long bookId, CancellationToken cancellationToken = default);
	Task DeleteAllWishlistAsync(string userId, CancellationToken cancellationToken = default);
}
