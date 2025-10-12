using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ITeamBrandRepository
    {
        Task<TeamBrand> AddAsync(TeamBrand teamBrand);
        Task<IEnumerable<TeamBrand>> GetByTeamIdAsync(Guid teamId);
        Task<IEnumerable<TeamBrand>> GetByBrandIdAsync(Guid brandId);
        Task<bool> DeleteAsync(Guid teamId, Guid brandId);
        Task<int> SoftDeleteByTeamIdAsync(Guid teamId);
        Task<int> CreateTeamBrandAssociationsAsync(Guid teamId, IEnumerable<Guid> brandIds, Guid userId);
    }
}
