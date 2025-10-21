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

        public async Task<PagedResult<Brand>> GetPagedBrandsByTeamMembershipAsync(Guid profileId, PaginationRequest request)
        {
            // Get brands where profile is the owner
            var ownerBrandsQuery = _context.Brands
                .Include(b => b.Profile)
                .Where(b => b.ProfileId == profileId && !b.IsDeleted);

            // Get brands where profile is a team member of the brand owner
            var teamBrandsQuery = _context.Brands
                .Include(b => b.Profile)
                .Where(b => !b.IsDeleted &&
                    _context.TeamBrands.Any(tb =>
                        tb.BrandId == b.Id &&
                        tb.IsActive &&
                        _context.TeamMembers.Any(tm =>
                            tm.UserId == profileId &&
                            tm.TeamId == tb.TeamId &&
                            tm.IsActive &&
                            tm.Team.ProfileId == b.ProfileId)));

            // Combine both queries
            var combinedQuery = ownerBrandsQuery.Union(teamBrandsQuery);

            // Apply search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                combinedQuery = combinedQuery.Where(b => b.Name.ToLower().Contains(request.SearchTerm.ToLower()));
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                combinedQuery = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDescending
                        ? combinedQuery.OrderByDescending(b => b.Name)
                        : combinedQuery.OrderBy(b => b.Name),
                    "createdat" => request.SortDescending
                        ? combinedQuery.OrderByDescending(b => b.CreatedAt)
                        : combinedQuery.OrderBy(b => b.CreatedAt),
                    _ => combinedQuery.OrderBy(b => b.Name)
                };
            }
            else
            {
                combinedQuery = combinedQuery.OrderBy(b => b.Name);
            }

            // Get total count
            var totalCount = await combinedQuery.CountAsync();

            // Apply pagination
            var data = await combinedQuery
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
    }
}