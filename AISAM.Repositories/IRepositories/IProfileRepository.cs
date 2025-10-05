using AISAM.Data.Model;
using AISAM.Data.Enumeration;

namespace AISAM.Repositories.IRepositories
{
    public interface IProfileRepository
    {
        Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Profile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Profile?> GetByUserIdAndTypeAsync(Guid userId, ProfileTypeEnum profileType, CancellationToken cancellationToken = default);
        Task<Profile> CreateAsync(Profile profile, CancellationToken cancellationToken = default);
        Task<Profile> UpdateAsync(Profile profile, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> UserHasProfileTypeAsync(Guid userId, ProfileTypeEnum profileType, CancellationToken cancellationToken = default);
    }
}