using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly AisamContext _db;

        public ProductRepository(AisamContext db)
        {
            _db = db;
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _db.Products
                .Include(p => p.Brand) // join brand nếu cần
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<PagedResult<Product>> GetPagedAsync(PaginationRequest request)
        {
            var query = _db.Products
                .Include(p => p.Brand)
                .Where(p => !p.IsDeleted);

            // search theo name
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(p => p.Name.ToLower().Contains(request.SearchTerm.ToLower()));
            }

            // sort
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDescending
                        ? query.OrderByDescending(p => p.Name)
                        : query.OrderBy(p => p.Name),
                    "price" => request.SortDescending
                        ? query.OrderByDescending(p => p.Price)
                        : query.OrderBy(p => p.Price),
                    "createdat" => request.SortDescending
                        ? query.OrderByDescending(p => p.CreatedAt)
                        : query.OrderBy(p => p.CreatedAt),
                    _ => query.OrderBy(p => p.Name)
                };
            }
            else
            {
                query = query.OrderBy(p => p.Name);
            }

            // total count
            var totalCount = await query.CountAsync();

            // paging
            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Product>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<Product> AddAsync(Product product)
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            _db.Products.Update(product);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null || product.IsDeleted) return false;

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;
            _db.Products.Update(product);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BrandExistsAsync(Guid brandId)
        {
            return await _db.Brands.AnyAsync(b => b.Id == brandId && !b.IsDeleted);
        }
    }
}
