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
                .Include(b => b.User)
                .Include(b => b.Profile)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        }

        public async Task<Brand?> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _context.Brands
                .Include(b => b.User)
                .Include(b => b.Profile)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<PagedResult<Brand>> GetPagedByUserIdAsync(Guid userId, PaginationRequest request)
        {
            var query = _context.Brands
                .Include(b => b.User)
                .Include(b => b.Profile)
                .Where(b => b.UserId == userId && !b.IsDeleted);

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

        public async Task<bool> UserExistsAsync(Guid userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }
    }
}