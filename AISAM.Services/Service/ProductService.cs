using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace AISAM.Services.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly SupabaseStorageService _supabaseService;

        public ProductService(IProductRepository productRepository, SupabaseStorageService supabaseService)
        {
            _productRepository = productRepository;
            _supabaseService = supabaseService;
        }

        /// <summary>
        /// Lấy danh sách product theo phân trang, có hỗ trợ search và sắp xếp
        /// </summary>
        public async Task<PagedResult<ProductResponseDto>> GetPagedAsync(PaginationRequest request)
        {
            var products = await _productRepository.GetPagedAsync(request);

            return new PagedResult<ProductResponseDto>
            {
                Data = products.Data.Select(MapToResponse).ToList(),
                TotalCount = products.TotalCount,
                Page = products.Page,
                PageSize = products.PageSize
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết product theo Id
        /// </summary>
        public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null || product.IsDeleted) return null;

            return MapToResponse(product);
        }

        /// <summary>
        /// Tạo mới product, kèm upload hình nếu có
        /// </summary>
        public async Task<ProductResponseDto> CreateAsync(ProductCreateRequest dto, List<IFormFile>? imageFiles = null)
        {
            // Kiểm tra Brand tồn tại
            if (!await _productRepository.BrandExistsAsync(dto.BrandId))
                throw new Exception("Brand not found.");

            // Upload ảnh lên Supabase nếu có
            var imageUrls = new List<string>();
            if (imageFiles != null)
            {
                foreach (var file in imageFiles)
                {
                    var url = await _supabaseService.UploadFileAsync(file);
                    if (!string.IsNullOrEmpty(url))
                        imageUrls.Add(url);
                }
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                BrandId = dto.BrandId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Images = JsonSerializer.Serialize(imageUrls),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _productRepository.AddAsync(product);
            return MapToResponse(created);
        }

        /// <summary>
        /// Cập nhật thông tin product theo Id, hỗ trợ cập nhật Brand, Name, Description, Price, ảnh
        /// </summary>
        public async Task<ProductResponseDto?> UpdateAsync(Guid id, ProductUpdateRequestDto dto)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null || product.IsDeleted) return null;

            // Cập nhật Brand nếu có
            if (dto.BrandId.HasValue)
            {
                if (!await _productRepository.BrandExistsAsync(dto.BrandId.Value))
                    throw new Exception("Brand not found.");
                product.BrandId = dto.BrandId.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
                product.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                product.Description = dto.Description;

            if (dto.Price.HasValue)
                product.Price = dto.Price.Value;

            // Lấy danh sách ảnh hiện tại
            var currentImages = !string.IsNullOrEmpty(product.Images)
                ? JsonSerializer.Deserialize<List<string>>(product.Images) ?? new List<string>()
                : new List<string>();

            // Upload ảnh mới và append
            if (dto.ImageFiles != null && dto.ImageFiles.Any())
            {
                foreach (var file in dto.ImageFiles)
                {
                    var url = await _supabaseService.UploadFileAsync(file);
                    if (!string.IsNullOrEmpty(url))
                        currentImages.Add(url);
                }
            }

            product.Images = JsonSerializer.Serialize(currentImages);
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);
            return MapToResponse(product);
        }

        /// <summary>
        /// Soft delete product (chỉ đánh dấu IsDeleted = true, không xóa thực sự)
        /// </summary>
        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null || product.IsDeleted) return false;

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);
            return true;
        }

        /// <summary>
        /// Khôi phục product đã xóa mềm
        /// </summary>
        public async Task<bool> RestoreAsync(Guid id)
        {
            var product = await _productRepository.GetByIdIncludingDeletedAsync(id);
            if (product == null || !product.IsDeleted) return false;

            product.IsDeleted = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);
            return true;
        }

        /// <summary>
        /// Chuyển Product sang ProductResponseDto
        /// </summary>
        private static ProductResponseDto MapToResponse(Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                BrandId = product.BrandId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Images = !string.IsNullOrEmpty(product.Images)
                    ? JsonSerializer.Deserialize<List<string>>(product.Images) ?? new List<string>()
                    : new List<string>(),
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
    }
}
