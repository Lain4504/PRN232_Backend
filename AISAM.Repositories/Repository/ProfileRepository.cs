using AISAM.Data.Model;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly AisamContext _context;

        public ProfileRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Profiles
                .Include(p => p.User)
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status != ProfileStatusEnum.Cancelled, cancellationToken);
        }

        public async Task<Profile?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Profiles
                .Include(p => p.User)
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Profile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Profiles
                .Include(p => p.User)
                .Include(p => p.Brands)
                .Where(p => p.UserId == userId && p.Status != ProfileStatusEnum.Cancelled)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Profile>> GetByUserIdIncludingDeletedAsync(Guid userId, bool isDeleted, CancellationToken cancellationToken = default)
        {
            var statusFilter = isDeleted ? ProfileStatusEnum.Cancelled : ProfileStatusEnum.Pending;
            return await _context.Profiles
                .Include(p => p.User)
                .Include(p => p.Brands)
                .Where(p => p.UserId == userId && p.Status == statusFilter)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<Profile> CreateAsync(Profile profile, CancellationToken cancellationToken = default)
        {
            profile.CreatedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync(cancellationToken);
            return profile;
        }

        public async Task<Profile> UpdateAsync(Profile profile, CancellationToken cancellationToken = default)
        {
            profile.UpdatedAt = DateTime.UtcNow;
            _context.Profiles.Update(profile);
            await _context.SaveChangesAsync(cancellationToken);
            return profile;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.Id == id && p.Status != ProfileStatusEnum.Cancelled, cancellationToken);

            if (profile != null)
            {
                profile.Status = ProfileStatusEnum.Cancelled;
                profile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }

        public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProfileStatusEnum.Cancelled, cancellationToken);

            if (profile != null)
            {
                profile.Status = ProfileStatusEnum.Pending; // Restore to pending status
                profile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Profiles
                .AnyAsync(p => p.Id == id && p.Status != ProfileStatusEnum.Cancelled, cancellationToken);
        }

        public async Task<IEnumerable<Profile>> SearchUserProfilesAsync(Guid userId, string? searchTerm = null, bool? isDeleted = null, CancellationToken cancellationToken = default)
        {
            // Start with a base query that includes both owned and shared profiles
            var query = _context.Profiles
                .Include(p => p.User)
                .Include(p => p.Brands)
                .Where(p => p.UserId == userId || 
                            _context.TeamMembers.Any(tm => tm.UserId == userId && 
                                                         tm.IsActive && 
                                                         tm.Team.ProfileId == p.Id && 
                                                         !tm.Team.IsDeleted));

            // Apply deletion status filter
            if (isDeleted.HasValue)
            {
                if (isDeleted.Value)
                {
                    // Specifically requested deleted profiles
                    query = query.Where(p => p.Status == ProfileStatusEnum.Cancelled);
                }
                else
                {
                    // Specifically requested active (non-deleted) profiles: Pending, Active, Suspended
                    query = query.Where(p => p.Status != ProfileStatusEnum.Cancelled);
                }
            }
            else
            {
                // Default behavior: only get non-deleted profiles
                query = query.Where(p => p.Status != ProfileStatusEnum.Cancelled);
            }

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchPattern = $"%{searchTerm}%";
                query = query.Where(p => 
                    (p.Name != null && EF.Functions.ILike(p.Name, searchPattern)) ||
                    (p.CompanyName != null && EF.Functions.ILike(p.CompanyName, searchPattern)) ||
                    (p.Bio != null && EF.Functions.ILike(p.Bio, searchPattern)));
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}