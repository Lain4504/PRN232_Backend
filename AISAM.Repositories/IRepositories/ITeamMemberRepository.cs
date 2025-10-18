using AISAM.Common.Dtos;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ITeamMemberRepository
    {
        Task<IEnumerable<TeamMember>> GetByTeamIdAsync(Guid teamId);
        Task<TeamMember?> GetByTeamAndUserAsync(Guid teamId, Guid userId);
        Task<IEnumerable<TeamMember>> GetByVendorIdAsync(Guid vendorId);
        Task<PagedResult<TeamMember>> GetPagedAsync(PaginationRequest request);
        Task<TeamMember?> GetByIdAsync(Guid id);
        Task<TeamMember?> GetByUserIdAsync(Guid userId);
        Task<TeamMember?> GetByUserIdAndBrandAsync(Guid userId, Guid brandId);
        Task<List<TeamMember>> GetByUserIdWithBrandsAsync(Guid userId);
        Task<TeamMember> AddAsync(TeamMember entity);
        Task UpdateAsync(TeamMember entity);
        Task<bool> DeleteAsync(Guid id);
        Task<int> DeleteByTeamIdAsync(Guid teamId);
        Task<int> SoftDeleteByTeamIdAsync(Guid teamId);
        Task<int> RestoreByTeamIdAsync(Guid teamId);

        Task<bool> TeamExistsAsync(Guid teamId);
        Task<bool> UserExistsAsync(Guid userId);
        Task<bool> IsUserMemberOfProfileTeamsAsync(Guid userId, Guid profileId);
    }
}


