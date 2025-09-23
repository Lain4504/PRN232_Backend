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
    public class CollectionService : ICollectionService
    {
        private readonly ICollectionRepository _repo;

        public CollectionService(ICollectionRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Collection>> GetAllAsync()
            => await _repo.GetAllAsync();

        public async Task<Collection?> GetByIdAsync(long id)
            => await _repo.GetByIdAsync(id);

        public async Task<Collection> CreateAsync(Collection collection)
        {
            collection.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(collection);
            await _repo.SaveChangesAsync();
            return collection;
        }

        public async Task UpdateAsync(Collection collection)
        {
            collection.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(collection);
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(Collection collection)
        {
            await _repo.DeleteAsync(collection);
            await _repo.SaveChangesAsync();
        }
    }
}
