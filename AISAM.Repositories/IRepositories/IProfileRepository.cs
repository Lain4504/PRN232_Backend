using AISAM.Data.Model;
using AISAM.Data.Enumeration;

namespace AISAM.Repositories.IRepositories
{
    public interface IProfileRepository
    {
        Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Profile?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Profile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Profile>> GetByUserIdAsync(Guid userId, bool isDeleted, CancellationToken cancellationToken = default);
        Task<IEnumerable<Profile>> GetByUserIdAndTypeAsync(Guid userId, ProfileTypeEnum profileType, CancellationToken cancellationToken = default);
        Task<IEnumerable<Profile>> GetByUserIdAndTypeAsync(Guid userId, ProfileTypeEnum profileType, bool isDeleted, CancellationToken cancellationToken = default);
        Task<Profile?> GetSingleByUserIdAndTypeAsync(Guid userId, ProfileTypeEnum profileType, CancellationToken cancellationToken = default);
        Task<Profile?> GetSingleByUserIdAndTypeAsync(Guid userId, ProfileTypeEnum profileType, bool isDeleted, CancellationToken cancellationToken = default);
        Task<Profile> CreateAsync(Profile profile, CancellationToken cancellationToken = default);
        Task<Profile> UpdateAsync(Profile profile, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> PermanentDeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}