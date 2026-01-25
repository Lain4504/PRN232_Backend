using AISAM.Common.Dtos;
using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class BrandRepository : IBrandRepository
    {
        private readonly AisamContext _context;

        public BrandRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Brand?> GetByIdAsync(Guid id)
        {
            return await _context.Brands
                .Include(b => b.Profile)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        }

        public async Task<Brand?> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _context.Brands
                .Include(b => b.Profile)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<PagedResult<Brand>> GetPagedByProfileIdAsync(Guid profileId, PaginationRequest request)
        {
            var query = _context.Brands
                .Include(b => b.Profile)
                .Where(b => b.ProfileId == profileId && !b.IsDeleted);

            // search theo name
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(b => b.Name.ToLower().Contains(request.SearchTerm.ToLower()));
            }

            // sort
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDescending
                        ? query.OrderByDescending(b => b.Name)
                        : query.OrderBy(b => b.Name),
                    "createdat" => request.SortDescending
                        ? query.OrderByDescending(b => b.CreatedAt)
                        : query.OrderBy(b => b.CreatedAt),
                    _ => query.OrderBy(b => b.Name)
                };
            }
            else
            {
                query = query.OrderBy(b => b.Name);
            }

            // total count
            var totalCount = await query.CountAsync();

            // paging
            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Brand>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<Brand> AddAsync(Brand brand)
        {
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
            return brand;
        }

        public async Task UpdateAsync(Brand brand)
        {
            _context.Brands.Update(brand);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ProfileExistsAsync(Guid profileId)
        {
            return await _context.Profiles.AnyAsync(p => p.Id == profileId);
        }

        public async Task<PagedResult<Brand>> GetPagedBrandsByTeamMembershipAsync(Guid profileId, Guid userId, PaginationRequest request, Guid? teamId = null)
        {
            // Get brands where user is a team member of the brand owner's teams and those teams are assigned to the brand
            var query = _context.Brands
                .Include(b => b.Profile)
                .Where(b => !b.IsDeleted && b.ProfileId == profileId &&
                    _context.TeamBrands.Any(tb =>
                        tb.BrandId == b.Id &&
                        tb.IsActive &&
                        (teamId == null || tb.TeamId == teamId) &&
                        _context.TeamMembers.Any(tm =>
                            tm.UserId == userId && 
                            tm.TeamId == tb.TeamId &&
                            tm.IsActive)));

            // Apply search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(b => b.Name.ToLower().Contains(request.SearchTerm.ToLower()));
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDescending
                        ? query.OrderByDescending(b => b.Name)
                        : query.OrderBy(b => b.Name),
                    "createdat" => request.SortDescending
                        ? query.OrderByDescending(b => b.CreatedAt)
                        : query.OrderBy(b => b.CreatedAt),
                    _ => query.OrderBy(b => b.Name)
                };
            }
            else
            {
                query = query.OrderBy(b => b.Name);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Brand>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<bool> UserExistsAsync(Guid userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<IEnumerable<Brand>> GetBrandsByTeamIdAsync(Guid teamId)
        {
            return await _context.Brands
                .Include(b => b.Profile)
                .Where(b => !b.IsDeleted &&
                    _context.TeamBrands.Any(tb =>
                        tb.BrandId == b.Id &&
                        tb.TeamId == teamId &&
                        tb.IsActive))
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Brand>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any())
            {
                return new List<Brand>();
            }

            return await _context.Brands
                .Include(b => b.Profile)
                .Where(b => idList.Contains(b.Id) && !b.IsDeleted)
                .ToListAsync();
        }
    }
}