using BookStore.Data;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repositories.Repository
{
    public class BookRepository : IBookRepository
    {
        private readonly BookStoreDbContext _context;
        public BookRepository(BookStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Book>> GetAllAsync()
            => await _context.Books.AsNoTracking().ToListAsync();

        public async Task<Book?> GetByIdAsync(long id)
            => await _context.Books.FindAsync(id);

        public async Task AddAsync(Book book)
            => await _context.Books.AddAsync(book);

        public Task UpdateAsync(Book book)
        {
            _context.Books.Update(book);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Book book)
        {
            _context.Books.Remove(book);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
