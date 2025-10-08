using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;

namespace AISAM.Services.Service
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUserRepository _userRepository;

        public TeamService(ITeamRepository teamRepository, IUserRepository userRepository)
        {
            _teamRepository = teamRepository;
            _userRepository = userRepository;
        }

        public async Task<TeamResponseDto?> GetByIdAsync(Guid id, Guid currentUserId)
        {
            var me = await _userRepository.GetByIdAsync(currentUserId) ?? throw new UnauthorizedAccessException("User not found");
            var team = await _teamRepository.GetByIdAsync(id);
            if (team == null || team.IsDeleted) return null;

            var canView = me.Role == UserRoleEnum.Admin
                          || team.VendorId == currentUserId
                          || await _teamRepository.IsMemberAsync(team.Id, currentUserId);
            if (!canView) return null;

            var dto = MapToResponse(team);

            // Optional detail restriction: only vendor/admin see description
            if (!(me.Role == UserRoleEnum.Admin || team.VendorId == currentUserId))
            {
                dto.Description = null;
            }
            return dto;
        }

        public async Task<PagedResult<TeamResponseDto>> GetPagedAsync(Guid currentUserId, PaginationRequest request)
        {
            var me = await _userRepository.GetByIdAsync(currentUserId) ?? throw new UnauthorizedAccessException("User not found");

            PagedResult<Team> page;
            if (me.Role == UserRoleEnum.Admin)
            {
                page = await _teamRepository.GetPagedForAdminAsync(request);
            }
            else if (me.Role == UserRoleEnum.Vendor)
            {
                page = await _teamRepository.GetPagedForVendorAsync(currentUserId, request);
            }
            else
            {
                // Regular user: only teams where user is a member
                page = await _teamRepository.GetPagedForMemberAsync(currentUserId, request);
            }

            var data = page.Data.Select(t =>
            {
                var dto = MapToResponse(t);
                if (!(me.Role == UserRoleEnum.Admin || t.VendorId == currentUserId))
                {
                    dto.Description = null;
                }
                return dto;
            }).ToList();

            return new PagedResult<TeamResponseDto>
            {
                Data = data,
                TotalCount = page.TotalCount,
                Page = page.Page,
                PageSize = page.PageSize
            };
        }

        public async Task<TeamResponseDto> CreateAsync(Guid currentUserId, CreateTeamRequest request)
        {
            var me = await _userRepository.GetByIdAsync(currentUserId) ?? throw new UnauthorizedAccessException("User not found");
            if (me.Role != UserRoleEnum.Vendor && me.Role != UserRoleEnum.Admin)
            {
                throw new UnauthorizedAccessException("Only Vendor or Admin can create team");
            }

            var vendorId = me.Role == UserRoleEnum.Admin
                ? (request.VendorId ?? currentUserId)
                : currentUserId; // Vendor cannot assign to others

            // Ensure vendor exists
            if (!await _teamRepository.UserExistsAsync(vendorId))
                throw new ArgumentException("Vendor not found");

            var team = new Team
            {
                Id = Guid.NewGuid(),
                VendorId = vendorId,
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
            };

            var created = await _teamRepository.AddAsync(team);
            return MapToResponse(created);
        }

        public async Task<TeamResponseDto?> UpdateAsync(Guid id, Guid currentUserId, UpdateTeamRequest request)
        {
            var me = await _userRepository.GetByIdAsync(currentUserId) ?? throw new UnauthorizedAccessException("User not found");
            var team = await _teamRepository.GetByIdAsync(id);
            if (team == null || team.IsDeleted) return null;

            if (me.Role == UserRoleEnum.Admin || team.VendorId == currentUserId)
            {
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    team.Name = request.Name;
                }
                // description can be null/empty
                team.Description = request.Description;

                await _teamRepository.UpdateAsync(team);
                return MapToResponse(team);
            }

            // Regular user not allowed
            throw new UnauthorizedAccessException("Not allowed to update team");
        }

        public async Task<bool> SoftDeleteAsync(Guid id, Guid currentUserId)
        {
            var me = await _userRepository.GetByIdAsync(currentUserId) ?? throw new UnauthorizedAccessException("User not found");
            var team = await _teamRepository.GetByIdAsync(id);
            if (team == null || team.IsDeleted) return false;

            if (me.Role == UserRoleEnum.Admin || team.VendorId == currentUserId)
            {
                team.IsDeleted = true;
                await _teamRepository.UpdateAsync(team);
                return true;
            }

            throw new UnauthorizedAccessException("Not allowed to delete team");
        }

        private static TeamResponseDto MapToResponse(Team team)
        {
            return new TeamResponseDto
            {
                Id = team.Id,
                VendorId = team.VendorId,
                Name = team.Name,
                Description = team.Description,
                IsDeleted = team.IsDeleted,
                CreatedAt = team.CreatedAt,
            };
        }
    }
}
