using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface IBrandService
    {
        Task<PagedResult<BrandResponseDto>> GetPagedByProfileIdAsync(Guid profileId, PaginationRequest request);

        Task<PagedResult<BrandResponseDto>> GetPagedBrandsByTeamMembershipAsync(Guid profileId, PaginationRequest request);

        Task<BrandResponseDto?> GetByIdAsync(Guid id, Guid profileId);

        Task<BrandResponseDto> CreateAsync(Guid profileId, CreateBrandRequest dto);

        Task<BrandResponseDto?> UpdateAsync(Guid id, Guid profileId, UpdateBrandRequest dto);

        Task<bool> SoftDeleteAsync(Guid id, Guid profileId);

        Task<bool> RestoreAsync(Guid id, Guid profileId);
    }
}