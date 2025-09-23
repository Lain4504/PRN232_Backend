using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using BookStore.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Services.Service
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _repo;

        public BookService(IBookRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Book>> GetAllAsync()
            => await _repo.GetAllAsync();

        public async Task<Book?> GetByIdAsync(long id)
            => await _repo.GetByIdAsync(id);

        public async Task<Book> CreateAsync(Book book)
        {
            book.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(book);
            await _repo.SaveChangesAsync();
            return book;
        }

        public async Task UpdateAsync(Book book)
        {
            book.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(book);
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(Book book)
        {
            await _repo.DeleteAsync(book);
            await _repo.SaveChangesAsync();
        }
    }
}
