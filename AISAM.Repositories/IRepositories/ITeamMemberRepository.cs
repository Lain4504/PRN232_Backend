using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ITeamMemberRepository
    {
        Task<IEnumerable<TeamMember>> GetByTeamIdAsync(Guid teamId);
        Task<IEnumerable<TeamMember>> GetByUserIdAsync(Guid userId);
        Task<TeamMember?> GetByTeamAndUserAsync(Guid teamId, Guid userId);
        Task<IEnumerable<TeamMember>> GetByVendorIdAsync(Guid vendorId);
    }
}


