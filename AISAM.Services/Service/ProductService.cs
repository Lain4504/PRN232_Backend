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

        public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null || product.IsDeleted) return null;
            return MapToResponse(product);
        }

        public async Task<ProductResponseDto> CreateAsync(ProductCreateRequest dto, List<IFormFile>? imageFiles = null)
        {
            // Validate BrandId có tồn tại
            if (!await _productRepository.BrandExistsAsync(dto.BrandId))
                throw new Exception("Brand not found.");

            // Upload ảnh nếu có
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

        public async Task<ProductResponseDto?> UpdateAsync(Guid id, ProductUpdateRequestDto dto, List<IFormFile>? newImages = null)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null || product.IsDeleted) return null;

            if (dto.BrandId.HasValue)
            {
                if (!await _productRepository.BrandExistsAsync(dto.BrandId.Value))
                    throw new Exception("Brand not found.");
                product.BrandId = dto.BrandId.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Name)) product.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Description)) product.Description = dto.Description;
            if (dto.Price.HasValue) product.Price = dto.Price.Value;

            // Append ảnh mới nếu có
            var currentImages = !string.IsNullOrEmpty(product.Images)
                ? JsonSerializer.Deserialize<List<string>>(product.Images) ?? new List<string>()
                : new List<string>();

            if (newImages != null && newImages.Any())
            {
                foreach (var file in newImages)
                {
                    var url = await _supabaseService.UploadFileAsync(file);
                    if (!string.IsNullOrEmpty(url))
                        currentImages.Add(url);
                }
            }

            if (dto.Images != null) currentImages = dto.Images;

            product.Images = JsonSerializer.Serialize(currentImages);
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);
            return MapToResponse(product);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null || product.IsDeleted) return false;

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);
            return true;
        }

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
