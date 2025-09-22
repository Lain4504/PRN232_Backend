using BookStore.Data.Model;

namespace BookStore.Repositories.IRepositories
{
    public interface IAuthorRepository
    {
        Task<Author?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Author>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Author> CreateAsync(Author author, CancellationToken cancellationToken = default);
        Task<Author> UpdateAsync(Author author, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);
    }
}