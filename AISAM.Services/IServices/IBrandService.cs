using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface IBrandService
    {
        Task<PagedResult<BrandResponseDto>> GetPagedByUserIdAsync(Guid userId, PaginationRequest request);

        Task<BrandResponseDto?> GetByIdAsync(Guid id);

        Task<BrandResponseDto> CreateAsync(Guid userId, CreateBrandRequest dto);

        Task<BrandResponseDto?> UpdateAsync(Guid id, Guid userId, UpdateBrandRequest dto);

        Task<bool> SoftDeleteAsync(Guid id, Guid userId);

        Task<bool> RestoreAsync(Guid id, Guid userId);
    }
}