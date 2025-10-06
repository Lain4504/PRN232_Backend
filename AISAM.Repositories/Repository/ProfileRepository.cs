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
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
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
                .Where(p => p.UserId == userId && !p.IsDeleted)
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
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (profile != null)
            {
                profile.IsDeleted = true;
                profile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }

        public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted, cancellationToken);

            if (profile != null)
            {
                profile.IsDeleted = false;
                profile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Profiles
                .AnyAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        }
    }
}