using BookStore.Data.Model;

namespace BookStore.Services
{
    public interface IWishlistService
    {
    Task<List<Wishlist>> GetWishlistByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default);
    Task DeleteWishlistAsync(long id, CancellationToken cancellationToken = default);
    Task DeleteAllWishlistAsync(string userId, CancellationToken cancellationToken = default);
    }
}
