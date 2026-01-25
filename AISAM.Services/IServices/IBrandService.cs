using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface IBrandService
    {
        Task<PagedResult<BrandResponseDto>> GetPagedByProfileIdAsync(Guid profileId, Guid userId, PaginationRequest request, Guid? teamId = null);

        Task<PagedResult<BrandResponseDto>> GetPagedBrandsByTeamMembershipAsync(Guid profileId, Guid userId, PaginationRequest request);

        Task<IEnumerable<BrandResponseDto>> GetBrandsByTeamIdAsync(Guid teamId, Guid userId);

        Task<PagedResult<ContentResponseDto>> GetPagedContentsByBrandIdAsync(Guid brandId, PaginationRequest request);

        Task<BrandResponseDto?> GetByIdAsync(Guid id, Guid userId);

        Task<BrandResponseDto> CreateAsync(Guid profileId, CreateBrandRequest dto);

        Task<BrandResponseDto?> UpdateAsync(Guid id, Guid userId, UpdateBrandRequest dto);

        Task<bool> SoftDeleteAsync(Guid id, Guid userId);

        Task<bool> RestoreAsync(Guid id, Guid userId);
    }
}