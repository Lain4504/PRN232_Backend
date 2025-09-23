using BookStore.Data;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repositories.Repository
{
    public class CollectionRepository : ICollectionRepository
    {
        private readonly BookStoreDbContext _context;
        public CollectionRepository(BookStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Collection>> GetAllAsync()
            => await _context.Collections.AsNoTracking().ToListAsync();

        public async Task<Collection?> GetByIdAsync(long id)
            => await _context.Collections.FindAsync(id);

        public async Task AddAsync(Collection collection)
            => await _context.Collections.AddAsync(collection);

        public Task UpdateAsync(Collection collection)
        {
            _context.Collections.Update(collection);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Collection collection)
        {
            _context.Collections.Remove(collection);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
