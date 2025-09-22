using BookStore.Data.Model;

namespace BookStore.Services.IServices
{
    public interface IPublisherService
    {
        Task<Publisher?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Publisher>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Publisher> CreatePublisherAsync(Publisher publisher, CancellationToken cancellationToken = default);
        Task<Publisher> UpdatePublisherAsync(Publisher publisher, CancellationToken cancellationToken = default);
        Task<bool> DeletePublisherAsync(long id, CancellationToken cancellationToken = default);
    }
}