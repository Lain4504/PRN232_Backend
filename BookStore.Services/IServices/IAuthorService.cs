using BookStore.Data.Model;

namespace BookStore.Services.IServices
{
    public interface IAuthorService
    {
        Task<Author?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Author>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Author> CreateAuthorAsync(Author author, CancellationToken cancellationToken = default);
        Task<Author> UpdateAuthorAsync(Author author, CancellationToken cancellationToken = default);
        Task<bool> DeleteAuthorAsync(long id, CancellationToken cancellationToken = default);
    }
}