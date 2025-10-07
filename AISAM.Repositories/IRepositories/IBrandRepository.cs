using AISAM.Common.Dtos;
using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IBrandRepository
    {
        Task<Brand?> GetByIdAsync(Guid id);
        Task<Brand?> GetByIdIncludingDeletedAsync(Guid id);
        Task<PagedResult<Brand>> GetPagedByUserIdAsync(Guid userId, PaginationRequest request);
        Task<Brand> AddAsync(Brand brand);
        Task UpdateAsync(Brand brand);
        Task<bool> UserExistsAsync(Guid userId);
    }
}