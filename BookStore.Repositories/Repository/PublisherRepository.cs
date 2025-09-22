using BookStore.Data;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repositories.Repository
{
    public class PublisherRepository : IPublisherRepository
    {
        private readonly BookStoreDbContext _dbContext;

        public PublisherRepository(BookStoreDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Publisher?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Publishers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Publisher>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Publishers
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Publisher> CreateAsync(Publisher publisher, CancellationToken cancellationToken = default)
        {
            _dbContext.Publishers.Add(publisher);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return publisher;
        }

        public async Task<Publisher> UpdateAsync(Publisher publisher, CancellationToken cancellationToken = default)
        {
            _dbContext.Publishers.Update(publisher);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return publisher;
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var publisher = await _dbContext.Publishers.FindAsync(new object[] { id }, cancellationToken);
            if (publisher == null)
                return false;

            _dbContext.Publishers.Remove(publisher);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Publishers.AnyAsync(p => p.Id == id, cancellationToken);
        }
    }
}