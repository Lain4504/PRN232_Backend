using BookStore.Data.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Repositories.IRepositories
{
    public interface ICollectionRepository
    {
        Task<IEnumerable<Collection>> GetAllAsync();
        Task<Collection?> GetByIdAsync(long id);
        Task AddAsync(Collection collection);
        Task UpdateAsync(Collection collection);
        Task DeleteAsync(Collection collection);
        Task SaveChangesAsync();
    }
}
