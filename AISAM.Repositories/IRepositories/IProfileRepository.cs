using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IProfileRepository
    {
        Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Profile?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Profile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Profile>> GetByUserIdIncludingDeletedAsync(Guid userId, bool isDeleted, CancellationToken cancellationToken = default);
        Task<IEnumerable<Profile>> SearchUserProfilesAsync(Guid userId, string? searchTerm = null, bool? isDeleted = null, CancellationToken cancellationToken = default);
        Task<Profile> CreateAsync(Profile profile, CancellationToken cancellationToken = default);
        Task<Profile> UpdateAsync(Profile profile, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}