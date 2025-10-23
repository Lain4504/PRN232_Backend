using AISAM.Common.Dtos;
using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IBrandRepository
    {
        Task<Brand?> GetByIdAsync(Guid id);
        Task<Brand?> GetByIdIncludingDeletedAsync(Guid id);
        Task<PagedResult<Brand>> GetPagedByProfileIdAsync(Guid profileId, PaginationRequest request);
        Task<Brand> AddAsync(Brand brand);
        Task UpdateAsync(Brand brand);
        Task<bool> ProfileExistsAsync(Guid profileId);
        Task<PagedResult<Brand>> GetPagedBrandsByTeamMembershipAsync(Guid profileId, PaginationRequest request);
        Task<bool> UserExistsAsync(Guid userId);
        Task<IEnumerable<Brand>> GetBrandsByTeamIdAsync(Guid teamId);

    }
}