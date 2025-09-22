using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using BookStore.Services.IServices;

namespace BookStore.Services.Service
{
    public class PublisherService : IPublisherService
    {
        private readonly IPublisherRepository _publisherRepository;

        public PublisherService(IPublisherRepository publisherRepository)
        {
            _publisherRepository = publisherRepository;
        }

        public Task<Publisher?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return _publisherRepository.GetByIdAsync(id, cancellationToken);
        }

        public Task<IEnumerable<Publisher>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _publisherRepository.GetAllAsync(cancellationToken);
        }

        public Task<Publisher> CreatePublisherAsync(Publisher publisher, CancellationToken cancellationToken = default)
        {
            return _publisherRepository.CreateAsync(publisher, cancellationToken);
        }

        public Task<Publisher> UpdatePublisherAsync(Publisher publisher, CancellationToken cancellationToken = default)
        {
            return _publisherRepository.UpdateAsync(publisher, cancellationToken);
        }

        public Task<bool> DeletePublisherAsync(long id, CancellationToken cancellationToken = default)
        {
            return _publisherRepository.DeleteAsync(id, cancellationToken);
        }
    }
}