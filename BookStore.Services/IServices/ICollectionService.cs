using BookStore.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Services.IServices
{
    public interface ICollectionService
    {
        Task<IEnumerable<Collection>> GetAllAsync();
        Task<Collection?> GetByIdAsync(long id);
        Task<Collection> CreateAsync(Collection collection);
        Task UpdateAsync(Collection collection);
        Task DeleteAsync(Collection collection);
    }
}
