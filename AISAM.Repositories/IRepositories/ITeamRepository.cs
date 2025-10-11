using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ITeamRepository
    {
        Task<Team> CreateAsync(Team team);
        Task<Team?> GetByIdAsync(Guid id);
        Task<IEnumerable<Team>> GetByVendorIdAsync(Guid vendorId, Guid userId);
        Task<bool> ExistsByNameAndVendorAsync(string name, Guid vendorId);
        Task<Team> UpdateAsync(Team team);
        Task DeleteAsync(Guid id);
    }
}