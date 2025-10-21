using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Data.Model;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
namespace AISAM.Services.Service
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ITeamBrandRepository _teamBrandRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;

        public TeamService(ITeamMemberRepository teamMemberRepository, ITeamRepository teamRepository, IUserRepository userRepository, IProfileRepository profileRepository, IBrandRepository brandRepository, ITeamBrandRepository teamBrandRepository, RolePermissionConfig rolePermissionConfig)
        {
            _teamMemberRepository = teamMemberRepository;
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _brandRepository = brandRepository;
            _teamBrandRepository = teamBrandRepository;
            _rolePermissionConfig = rolePermissionConfig;
        }

        public async Task<GenericResponse<TeamResponse>> CreateTeamAsync(CreateTeamRequest request, Guid profileId)
        {
            try
            {
                // Kiểm tra profile type: chỉ Basic và Pro mới có thể tạo team
                var profile = await _profileRepository.GetByIdAsync(profileId);
                if (profile == null)
                {
                    throw new UnauthorizedAccessException("Profile not found.");
                }

                if (profile.ProfileType == ProfileTypeEnum.Free)
                {
                    return GenericResponse<TeamResponse>.CreateError("Tính năng team chỉ dành cho gói Basic và Pro. Vui lòng nâng cấp gói để sử dụng tính năng này.");
                }

                // Kiểm tra tên team đã tồn tại chưa
                var existingTeam = await _teamRepository.ExistsByNameAndProfileAsync(request.Name, profileId);
                if (existingTeam)
                {
                    return GenericResponse<TeamResponse>.CreateError("Tên team đã tồn tại");
                }

                // Tạo team mới
                var team = new Team
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profileId,
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    Status = TeamStatusEnum.Active
                };

                var createdTeam = await _teamRepository.CreateAsync(team);
                if (request.BrandIds?.Any() == true)
                {
                    await _teamBrandRepository.CreateTeamBrandAssociationsAsync(createdTeam.Id, request.BrandIds, profileId);
                }

                try
                {
                    // Tạo TeamMember cho người tạo team với role Vendor
                    var teamMember = new TeamMember
                    {
                        Id = Guid.NewGuid(),
                        TeamId = createdTeam.Id,
                        UserId = profileId,
                        Role = "Vendor",
                        Permissions = _rolePermissionConfig.GetPermissionsByRole("Vendor"),
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _teamMemberRepository.AddAsync(teamMember);
                }
                catch (Exception ex)
                {
                    // Log lỗi tạo TeamMember nhưng không làm fail việc tạo team
                    Console.WriteLine($"Warning: Failed to create team member for user {profileId} in team {createdTeam.Id}: {ex.Message}");
                }

                var response = new TeamResponse
                {
                    Id = createdTeam.Id,
                    ProfileId = createdTeam.ProfileId,
                    Name = createdTeam.Name,
                    Description = createdTeam.Description,
                    CreatedAt = createdTeam.CreatedAt,
                    VendorEmail = profile?.User?.Email ?? "",
                };

                return GenericResponse<TeamResponse>.CreateSuccess(response, "Tạo team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<TeamResponse>.CreateError($"Lỗi khi lấy thông tin team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<TeamResponse>> GetTeamByIdAsync(Guid id, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "VIEW_TEAM_MEMBERS"))
                    throw new UnauthorizedAccessException("Bạn không có quyền xem thông tin team.");

                // Lấy thông tin team theo ID
                var team = await _teamRepository.GetByIdAsync(id);
                if (team == null)
                {
                    return GenericResponse<TeamResponse>.CreateError("Không tìm thấy team với ID được cung cấp.");
                }

                // Lấy thông tin vendor để có email
                var profile = await _profileRepository.GetByIdAsync(team.ProfileId);
                var response = new TeamResponse
                {
                    Id = team.Id,
                    ProfileId = team.ProfileId,
                    Name = team.Name,
                    Description = team.Description,
                    CreatedAt = team.CreatedAt,
                    VendorEmail = profile?.User?.Email ?? "",
                };

                return GenericResponse<TeamResponse>.CreateSuccess(response, "Lấy thông tin team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<TeamResponse>.CreateError($"Lỗi khi lấy thông tin team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<TeamResponse>>> GetTeamsByProfileAsync(Guid profileId, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "VIEW_TEAM_MEMBERS"))
                    throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách team.");

                // Lấy danh sách teams theo profile
                var teams = await _teamRepository.GetByProfileIdAsync(profileId, userId);

                // Lấy thông tin profile để có email
                var profile = await _profileRepository.GetByIdAsync(profileId);

                // Chuyển đổi sang TeamResponse với thông tin đầy đủ
                var teamResponses = new List<TeamResponse>();
                foreach (var team in teams)
                {
                    teamResponses.Add(new TeamResponse
                    {
                        Id = team.Id,
                        ProfileId = team.ProfileId,
                        Name = team.Name,
                        Description = team.Description,
                        CreatedAt = team.CreatedAt,
                        VendorEmail = profile?.User?.Email ?? "",
                    });
                }

                return GenericResponse<IEnumerable<TeamResponse>>.CreateSuccess(teamResponses, "Lấy danh sách team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<TeamResponse>>.CreateError($"Lỗi khi lấy danh sách team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<TeamResponse>> UpdateTeamAsync(Guid id, CreateTeamRequest request, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "UPDATE_TEAM"))
                    throw new UnauthorizedAccessException("Bạn không có quyền cập nhật team.");

                // Lấy thông tin team hiện tại
                var existingTeam = await _teamRepository.GetByIdAsync(id);
                if (existingTeam == null)
                {
                    return GenericResponse<TeamResponse>.CreateError("Không tìm thấy team để cập nhật.");
                }

                // Kiểm tra tên team mới có bị trùng không (chỉ khi tên thay đổi)
                if (existingTeam.Name.Trim() != request.Name.Trim())
                {
                    var nameExists = await _teamRepository.ExistsByNameAndProfileAsync(request.Name, existingTeam.ProfileId);
                    if (nameExists)
                    {
                        return GenericResponse<TeamResponse>.CreateError("Tên team đã tồn tại");
                    }
                }

                // Cập nhật thông tin team
                existingTeam.Name = request.Name.Trim();
                existingTeam.Description = request.Description?.Trim();

                var updatedTeam = await _teamRepository.UpdateAsync(existingTeam);

                // Lấy thông tin vendor để có email
                var profile = await _profileRepository.GetByIdAsync(updatedTeam.ProfileId);

                var response = new TeamResponse
                {
                    Id = updatedTeam.Id,
                    ProfileId = updatedTeam.ProfileId,
                    Name = updatedTeam.Name,
                    Description = updatedTeam.Description,
                    CreatedAt = updatedTeam.CreatedAt,
                    VendorEmail = profile?.User?.Email ?? "",
                };

                return GenericResponse<TeamResponse>.CreateSuccess(response, "Cập nhật team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<TeamResponse>.CreateError($"Lỗi khi cập nhật team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> DeleteTeamAsync(Guid id, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "DELETE_TEAM"))
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa team.");

                // Lấy thông tin team hiện tại để kiểm tra tồn tại
                var existingTeam = await _teamRepository.GetByIdAsync(id);
                if (existingTeam == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy team để xóa.");
                }

                // Kiểm tra team đã bị xóa mềm chưa
                if (existingTeam.IsDeleted)
                {
                    return GenericResponse<bool>.CreateError("Team đã được xóa trước đó.");
                }

                // Soft delete tất cả team members trước
                var deletedMembersCount = await _teamMemberRepository.SoftDeleteByTeamIdAsync(id);
                if (deletedMembersCount > 0)
                {
                    Console.WriteLine($"Soft deleted {deletedMembersCount} team members for team {id}");
                }

                // Soft delete tất cả team brand associations trước
                var deletedBrandsCount = await _teamBrandRepository.SoftDeleteByTeamIdAsync(id);
                if (deletedBrandsCount > 0)
                {
                    Console.WriteLine($"Soft deleted {deletedBrandsCount} team brand associations for team {id}");
                }

                // Soft delete team từ repository
                existingTeam.IsDeleted = true;
                await _teamRepository.UpdateAsync(existingTeam);

                return GenericResponse<bool>.CreateSuccess(true, "Xóa team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi xóa team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<TeamMemberResponseDto>>> GetTeamMembersAsync(Guid teamId, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "VIEW_TEAM_MEMBERS"))
                    throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách thành viên trong team.");

                // Lấy thông tin team để kiểm tra tồn tại
                var team = await _teamRepository.GetByIdAsync(teamId);
                if (team == null)
                {
                    return GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateError("Không tìm thấy team với ID được cung cấp.");
                }

                // Lấy danh sách thành viên trong team
                var teamMembers = await _teamMemberRepository.GetByTeamIdAsync(teamId);

                // Lấy thông tin user cho mỗi thành viên để có email
                var memberResponses = new List<TeamMemberResponseDto>();
                foreach (var member in teamMembers)
                {
                    var user = await _userRepository.GetByIdAsync(member.UserId);
                    memberResponses.Add(new TeamMemberResponseDto
                    {
                        Id = member.Id,
                        TeamId = member.TeamId,
                        UserId = member.UserId,
                        Role = member.Role,
                        Permissions = member.Permissions ?? new List<string>(),
                        JoinedAt = member.JoinedAt,
                        IsActive = member.IsActive,
                        UserEmail = user?.Email ?? ""
                    });
                }

                return GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateSuccess(memberResponses, "Lấy danh sách thành viên thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateError($"Lỗi khi lấy danh sách thành viên: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> AssignBrandToTeamAsync(Guid id, AssignBrandToTeamRequest request, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "UPDATE_TEAM"))
                    throw new UnauthorizedAccessException("Bạn không có quyền cập nhật team.");

                // Lấy thông tin team hiện tại
                var existingTeam = await _teamRepository.GetByIdAsync(id);
                if (existingTeam == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy team để cập nhật.");
                }

                // Kiểm tra team đã bị xóa mềm chưa
                if (existingTeam.IsDeleted)
                {
                    return GenericResponse<bool>.CreateError("Không thể cập nhật brand của team đã bị xóa.");
                }

                // Kiểm tra quyền sở hữu brand
                var brand = await _brandRepository.GetByIdAsync(request.BrandId);
                if (brand == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy brand với ID được cung cấp.");
                }

                if (brand.ProfileId != existingTeam.ProfileId)
                {
                    return GenericResponse<bool>.CreateError("Brand không thuộc về vendor của team.");
                }

                // Kiểm tra brand đã bị xóa mềm chưa
                if (brand.IsDeleted)
                {
                    return GenericResponse<bool>.CreateError("Không thể assign brand đã bị xóa.");
                }

                if (request.Assign)
                {
                    // Kiểm tra brand đã được assign cho team chưa
                    var existingAssociation = await _teamBrandRepository.GetByTeamAndBrandAsync(id, request.BrandId);
                    if (existingAssociation != null && existingAssociation.IsActive)
                    {
                        return GenericResponse<bool>.CreateError("Brand đã được assign cho team.");
                    }

                    // Assign brand cho team
                    var teamBrand = new TeamBrand
                    {
                        TeamId = id,
                        BrandId = request.BrandId,
                        IsActive = true
                    };
                    await _teamBrandRepository.AddAsync(teamBrand);
                }
                else
                {
                    // Unassign brand khỏi team (soft delete)
                    var existingAssociation = await _teamBrandRepository.GetByTeamAndBrandAsync(id, request.BrandId);
                    if (existingAssociation == null || !existingAssociation.IsActive)
                    {
                        return GenericResponse<bool>.CreateError("Brand chưa được assign cho team.");
                    }

                    existingAssociation.IsActive = false;
                    await _teamBrandRepository.UpdateAsync(existingAssociation);
                }

                return GenericResponse<bool>.CreateSuccess(true, $"{(request.Assign ? "Assign" : "Unassign")} brand cho team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi {(request.Assign ? "assign" : "unassign")} brand cho team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> UpdateTeamStatusAsync(Guid id, UpdateTeamStatusRequest request, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "UPDATE_TEAM"))
                    throw new UnauthorizedAccessException("Bạn không có quyền cập nhật team.");

                // Lấy thông tin team hiện tại
                var existingTeam = await _teamRepository.GetByIdAsync(id);
                if (existingTeam == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy team để cập nhật.");
                }

                // Kiểm tra team đã bị xóa mềm chưa
                if (existingTeam.IsDeleted)
                {
                    return GenericResponse<bool>.CreateError("Không thể cập nhật trạng thái của team đã bị xóa.");
                }

                // Cập nhật status
                existingTeam.Status = request.Status;
                await _teamRepository.UpdateAsync(existingTeam);

                return GenericResponse<bool>.CreateSuccess(true, "Cập nhật trạng thái team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi cập nhật trạng thái team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> RestoreTeamAsync(Guid id, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "UPDATE_TEAM"))
                    throw new UnauthorizedAccessException("Bạn không có quyền khôi phục team.");

                // Lấy thông tin team hiện tại (bao gồm cả đã bị xóa mềm)
                var existingTeam = await _teamRepository.GetByIdAsync(id);
                if (existingTeam == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy team để khôi phục.");
                }

                // Kiểm tra team đã bị xóa mềm chưa
                if (!existingTeam.IsDeleted)
                {
                    return GenericResponse<bool>.CreateError("Team chưa bị xóa, không cần khôi phục.");
                }

                // Khôi phục team và các thành phần liên quan
                existingTeam.IsDeleted = false;
                await _teamRepository.UpdateAsync(existingTeam);

                // Khôi phục tất cả team members
                var restoredMembersCount = await _teamMemberRepository.RestoreByTeamIdAsync(id);
                if (restoredMembersCount > 0)
                {
                    Console.WriteLine($"Restored {restoredMembersCount} team members for team {id}");
                }

                // Khôi phục tất cả team brand associations
                var restoredBrandsCount = await _teamBrandRepository.RestoreByTeamIdAsync(id);
                if (restoredBrandsCount > 0)
                {
                    Console.WriteLine($"Restored {restoredBrandsCount} team brand associations for team {id}");
                }

                return GenericResponse<bool>.CreateSuccess(true, "Khôi phục team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi khôi phục team: {ex.Message}");
            }
        }

        private async Task<List<string>> GetPermissionsByUserId(Guid userId)
        {
            var member = await _teamMemberRepository.GetByUserIdAsync(userId);
            if (member == null || member.Permissions == null || member.Permissions.Count == 0)
                return new List<string> {}; // default permissions
            return member.Permissions;
        }
    }
}