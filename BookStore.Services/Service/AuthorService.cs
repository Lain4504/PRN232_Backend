using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using BookStore.Services.IServices;

namespace BookStore.Services.Service
{
    public class AuthorService : IAuthorService
    {
        private readonly IAuthorRepository _authorRepository;

        public AuthorService(IAuthorRepository authorRepository)
        {
            _authorRepository = authorRepository;
        }

        public Task<Author?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return _authorRepository.GetByIdAsync(id, cancellationToken);
        }

        public Task<IEnumerable<Author>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _authorRepository.GetAllAsync(cancellationToken);
        }

        public Task<Author> CreateAuthorAsync(Author author, CancellationToken cancellationToken = default)
        {
            return _authorRepository.CreateAsync(author, cancellationToken);
        }

        public Task<Author> UpdateAuthorAsync(Author author, CancellationToken cancellationToken = default)
        {
            return _authorRepository.UpdateAsync(author, cancellationToken);
        }

        public Task<bool> DeleteAuthorAsync(long id, CancellationToken cancellationToken = default)
        {
            return _authorRepository.DeleteAsync(id, cancellationToken);
        }
    }
}