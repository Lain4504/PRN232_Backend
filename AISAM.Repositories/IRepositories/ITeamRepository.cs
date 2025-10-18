using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ITeamRepository
    {
        Task<Team> CreateAsync(Team team);
        Task<Team?> GetByIdAsync(Guid id);
        Task<IEnumerable<Team>> GetByProfileIdAsync(Guid profileId, Guid userId);
        Task<IEnumerable<Team>> GetTeamsByProfileAsync(Guid profileId);
        Task<bool> ExistsByNameAndProfileAsync(string name, Guid profileId);
        Task<Team> UpdateAsync(Team team);
        Task DeleteAsync(Guid id);
    }
}