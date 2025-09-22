using BookStore.Data.Model;

namespace BookStore.Repositories.IRepositories
{
    public interface IPublisherRepository
    {
        Task<Publisher?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Publisher>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Publisher> CreateAsync(Publisher publisher, CancellationToken cancellationToken = default);
        Task<Publisher> UpdateAsync(Publisher publisher, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);
    }
}