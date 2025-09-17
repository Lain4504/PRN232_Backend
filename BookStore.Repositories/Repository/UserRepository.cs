using BookStore.Data;
using BookStore.Data.Model;
using BookStore.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repositories.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly BookStoreDbContext _dbContext;

        public UserRepository(BookStoreDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }
    }
}
