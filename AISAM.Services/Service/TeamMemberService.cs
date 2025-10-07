using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;

namespace AISAM.Services.Service
{
    public class TeamMemberService : ITeamMemberService
    {
        private readonly ITeamMemberRepository _teamMemberRepository;

        public TeamMemberService(ITeamMemberRepository teamMemberRepository)
        {
            _teamMemberRepository = teamMemberRepository;
        }

        public async Task<PagedResult<TeamMemberResponseDto>> GetPagedAsync(PaginationRequest request)
        {
            var result = await _teamMemberRepository.GetPagedAsync(request);
            return new PagedResult<TeamMemberResponseDto>
            {
                Data = result.Data.Select(MapToResponse).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<TeamMemberResponseDto?> GetByIdAsync(Guid id)
        {
            var entity = await _teamMemberRepository.GetByIdAsync(id);
            return entity == null ? null : MapToResponse(entity);
        }

        public async Task<TeamMemberResponseDto> CreateAsync(TeamMemberCreateRequest request)
        {
            if (!await _teamMemberRepository.TeamExistsAsync(request.TeamId))
                throw new ArgumentException("Team not found");

            if (!await _teamMemberRepository.UserExistsAsync(request.UserId))
                throw new ArgumentException("User not found");

            var entity = new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = request.TeamId,
                UserId = request.UserId,
                Role = request.Role.ToString(),
                Permissions = request.Permissions ?? new List<string>(),
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            var created = await _teamMemberRepository.AddAsync(entity);
            return MapToResponse(created);
        }

        public async Task<TeamMemberResponseDto?> UpdateAsync(Guid id, TeamMemberUpdateRequest request)
        {
            var entity = await _teamMemberRepository.GetByIdAsync(id);
            if (entity == null) return null;

            if (request.TeamId.HasValue)
            {
                if (!await _teamMemberRepository.TeamExistsAsync(request.TeamId.Value))
                    throw new ArgumentException("Team not found");
                entity.TeamId = request.TeamId.Value;
            }

            if (request.Role.HasValue)
                entity.Role = request.Role.Value.ToString();

            if (request.Permissions != null && request.Permissions.Any())
                entity.Permissions = request.Permissions;

            if (request.IsActive.HasValue)
                entity.IsActive = request.IsActive.Value;

            await _teamMemberRepository.UpdateAsync(entity);
            return MapToResponse(entity);
        }

        public async Task<bool> DeleteAsync(Guid id) =>
            await _teamMemberRepository.DeleteAsync(id);

        private static TeamMemberResponseDto MapToResponse(TeamMember entity)
        {
            return new TeamMemberResponseDto
            {
                Id = entity.Id,
                TeamId = entity.TeamId,
                UserId = entity.UserId,
                Role = entity.Role,
                Permissions = entity.Permissions,
                JoinedAt = entity.JoinedAt,
                IsActive = entity.IsActive
            };
        }
    }
}
