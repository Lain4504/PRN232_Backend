using BookStore.Data;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repositories.Repository
{
    public class AuthorRepository : IAuthorRepository
    {
        private readonly BookStoreDbContext _dbContext;

        public AuthorRepository(BookStoreDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Author?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Authors
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Author>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Authors
                .AsNoTracking()
                .OrderBy(a => a.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Author> CreateAsync(Author author, CancellationToken cancellationToken = default)
        {
            _dbContext.Authors.Add(author);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return author;
        }

        public async Task<Author> UpdateAsync(Author author, CancellationToken cancellationToken = default)
        {
            _dbContext.Authors.Update(author);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return author;
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var author = await _dbContext.Authors.FindAsync(new object[] { id }, cancellationToken);
            if (author == null)
                return false;

            _dbContext.Authors.Remove(author);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Authors.AnyAsync(a => a.Id == id, cancellationToken);
        }
    }
}