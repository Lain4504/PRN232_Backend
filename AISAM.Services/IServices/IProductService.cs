using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Model;
using Microsoft.AspNetCore.Http;

namespace AISAM.Services.IServices
{
    public interface IProductService
    {
        Task<PagedResult<ProductResponseDto>> GetPagedAsync(PaginationRequest request);

        Task<ProductResponseDto?> GetByIdAsync(Guid id);

        Task<ProductResponseDto> CreateAsync(ProductCreateRequest dto, List<IFormFile>? imageFiles = null);

        Task<ProductResponseDto?> UpdateAsync(Guid id, ProductUpdateRequestDto dto, List<IFormFile>? newImages = null);

        Task<bool> DeleteAsync(Guid id);
    }
}
