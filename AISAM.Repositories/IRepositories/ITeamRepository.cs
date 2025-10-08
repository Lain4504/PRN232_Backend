using AISAM.Common.Dtos;
using AISAM.Common.Models;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ITeamRepository
    {
        Task<Team?> GetByIdAsync(Guid id);
        Task<Team?> GetByIdIncludingDeletedAsync(Guid id);
        Task<PagedResult<Team>> GetPagedForAdminAsync(PaginationRequest request);
        Task<PagedResult<Team>> GetPagedForVendorAsync(Guid vendorId, PaginationRequest request);
        Task<PagedResult<Team>> GetPagedForMemberAsync(Guid userId, PaginationRequest request);
        Task<Team> AddAsync(Team team);
        Task UpdateAsync(Team team);

        Task<bool> UserExistsAsync(Guid userId);
        Task<bool> IsMemberAsync(Guid teamId, Guid userId);
        Task<string?> GetMemberPermissionsJsonAsync(Guid teamId, Guid userId);
    }
}
