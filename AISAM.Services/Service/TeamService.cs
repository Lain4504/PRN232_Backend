using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Data.Model;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<TeamService> _logger;

        public TeamService(ITeamMemberRepository teamMemberRepository, ITeamRepository teamRepository, IUserRepository userRepository, IProfileRepository profileRepository, IBrandRepository brandRepository, ITeamBrandRepository teamBrandRepository, RolePermissionConfig rolePermissionConfig, ILogger<TeamService> logger)
        {
            _teamMemberRepository = teamMemberRepository;
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _brandRepository = brandRepository;
            _teamBrandRepository = teamBrandRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _logger = logger;
        }

        public async Task<GenericResponse<TeamResponse>> CreateTeamAsync(CreateTeamRequest request, Guid profileId, Guid userId)
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
                        UserId = userId, // Sử dụng userId thực tế
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
                    Console.WriteLine($"Warning: Failed to create team member for user {userId} in team {createdTeam.Id}: {ex.Message}");
                }

                var response = new TeamResponse
                {
                    Id = createdTeam.Id,
                    ProfileId = createdTeam.ProfileId,
                    Name = createdTeam.Name,
                    Description = createdTeam.Description,
                    CreatedAt = createdTeam.CreatedAt,
                    Status = createdTeam.Status,
                    MembersCount = 1 // Vendor is automatically added as first member
                };

                return GenericResponse<TeamResponse>.CreateSuccess(response, "Tạo team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<TeamResponse>.CreateError($"Lỗi khi lấy thông tin team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<TeamResponse>> GetTeamByIdAsync(Guid id, Guid profileId, Guid userId)
        {
            try
            {
                // Lấy thông tin team theo ID trước để kiểm tra quyền
                var team = await _teamRepository.GetByIdAsync(id);
                if (team == null)
                {
                    return GenericResponse<TeamResponse>.CreateError("Không tìm thấy team với ID được cung cấp.");
                }

                // Kiểm tra quyền: profile phải là chủ sở hữu của team đó hoặc user là member của team
                if (team.ProfileId == profileId)
                {
                    // Profile owner có thể xem team của chính mình
                }
                else
                {
                    // Kiểm tra xem user có phải là member của team không
                    var isTeamMember = await _teamMemberRepository.GetByTeamAndUserAsync(id, userId);
                    if (isTeamMember == null)
                    {
                        throw new UnauthorizedAccessException("Bạn không có quyền xem thông tin team.");
                    }
                }

                // Lấy số lượng members
                var membersCount = await _teamMemberRepository.GetByTeamIdAsync(team.Id);
                
                var response = new TeamResponse
                {
                    Id = team.Id,
                    ProfileId = team.ProfileId,
                    Name = team.Name,
                    Description = team.Description,
                    CreatedAt = team.CreatedAt,
                    Status = team.Status,
                    MembersCount = membersCount.Count()
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
                // Check ownership: does the user own the requested profile?
                var profile = await _profileRepository.GetByIdAsync(profileId);
                if (profile == null)
                {
                    return GenericResponse<IEnumerable<TeamResponse>>.CreateError("Profile không tồn tại");
                }

                bool isOwner = profile.UserId == userId;
                
                if (!isOwner)
                {
                    // If not owner, check if the user is an active team member of any team in this profile
                    var isTeamMember = await _teamMemberRepository.IsUserMemberOfProfileTeamsAsync(userId, profileId);
                    if (!isTeamMember)
                    {
                        return GenericResponse<IEnumerable<TeamResponse>>.CreateError("Bạn không có quyền truy cập đội nhóm của profile này");
                    }
                }

                var teams = await _teamRepository.GetTeamsByProfileAsync(profileId);
                var teamResponses = teams.Select(MapToResponse).ToList();
                return GenericResponse<IEnumerable<TeamResponse>>.CreateSuccess(teamResponses, "Lấy danh sách teams thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teams by profile {ProfileId}", profileId);
                return GenericResponse<IEnumerable<TeamResponse>>.CreateError($"Lỗi khi lấy danh sách teams: {ex.Message}");
            }
        }


        public async Task<GenericResponse<TeamResponse>> UpdateTeamAsync(Guid id, UpdateTeamRequest request, Guid userId)
        {
            try
            {
                // Validate quyền của user với team
                var permissionCheck = await ValidateUserTeamPermission(userId, id, "UPDATE_TEAM");
                if (!permissionCheck.Success)
                {
                    return GenericResponse<TeamResponse>.CreateError(permissionCheck.Message);
                }

                // Lấy thông tin team hiện tại
                var existingTeam = await _teamRepository.GetByIdAsync(id);
                if (existingTeam == null)
                {
                    return GenericResponse<TeamResponse>.CreateError("Không tìm thấy team để cập nhật.");
                }

                // Kiểm tra team đã bị xóa mềm chưa
                if (existingTeam.IsDeleted)
                {
                    return GenericResponse<TeamResponse>.CreateError("Không thể cập nhật team đã bị xóa.");
                }

                // Cập nhật thông tin team (chỉ cập nhật các field được cung cấp)
                if (!string.IsNullOrEmpty(request.Name))
                {
                    // Kiểm tra tên team mới có bị trùng không (chỉ khi tên thay đổi)
                    if (existingTeam.Name.Trim() != request.Name.Trim())
                    {
                        var nameExists = await _teamRepository.ExistsByNameAndProfileAsync(request.Name, existingTeam.ProfileId);
                        if (nameExists)
                        {
                            return GenericResponse<TeamResponse>.CreateError("Tên team đã tồn tại");
                        }
                    }
                    existingTeam.Name = request.Name.Trim();
                }

                if (request.Description != null)
                {
                    existingTeam.Description = request.Description.Trim();
                }

                if (request.Status.HasValue)
                {
                    existingTeam.Status = request.Status.Value;
                }

                var updatedTeam = await _teamRepository.UpdateAsync(existingTeam);

                var response = new TeamResponse
                {
                    Id = updatedTeam.Id,
                    ProfileId = updatedTeam.ProfileId,
                    Name = updatedTeam.Name,
                    Description = updatedTeam.Description,
                    CreatedAt = updatedTeam.CreatedAt,
                    Status = updatedTeam.Status
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
                // Validate quyền của user với team
                var permissionCheck = await ValidateUserTeamPermission(userId, id, "DELETE_TEAM");
                if (!permissionCheck.Success)
                {
                    return GenericResponse<bool>.CreateError(permissionCheck.Message);
                }

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

        public async Task<GenericResponse<IEnumerable<TeamMemberResponseDto>>> GetTeamMembersAsync(Guid teamId, Guid profileId, Guid userId)
        {
            try
            {
                // Lấy thông tin team để kiểm tra quyền
                var team = await _teamRepository.GetByIdAsync(teamId);
                if (team == null)
                {
                    return GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateError("Không tìm thấy team với ID được cung cấp.");
                }

                // Kiểm tra quyền: profile phải là chủ sở hữu của team đó hoặc user là member của team
                if (team.ProfileId == profileId)
                {
                    // Profile owner có thể xem team members của chính mình
                }
                else
                {
                    // Kiểm tra xem user có phải là member của team không
                    var isTeamMember = await _teamMemberRepository.GetByTeamAndUserAsync(teamId, userId);
                    if (isTeamMember == null)
                    {
                        throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách thành viên trong team.");
                    }
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
                        UserEmail = user?.Email ?? "",
                        CanApproveContent = (member.Permissions ?? new List<string>()).Contains("APPROVE_CONTENT")
                    });
                }

                return GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateSuccess(memberResponses, "Lấy danh sách thành viên thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateError($"Lỗi khi lấy danh sách thành viên: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<string>>> GetMyPermissionsAsync(Guid teamId, Guid userId)
        {
            try
            {
                var member = await _teamMemberRepository.GetByTeamAndUserAsync(teamId, userId);
                if (member == null || !member.IsActive)
                {
                    return GenericResponse<IEnumerable<string>>.CreateError("Bạn không phải là thành viên của team này.");
                }
                return GenericResponse<IEnumerable<string>>.CreateSuccess(member.Permissions ?? new List<string>(), "Lấy danh sách quyền thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<string>>.CreateError($"Lỗi khi lấy quyền: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> AssignBrandToTeamAsync(Guid id, AssignBrandToTeamRequest request, Guid userId)
        {
            try
            {
                // Validate quyền của user với team
                var permissionCheck = await ValidateUserTeamPermission(userId, id, "UPDATE_TEAM");
                if (!permissionCheck.Success)
                {
                    return GenericResponse<bool>.CreateError(permissionCheck.Message);
                }

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

                // Validate brand IDs
                if (request.BrandIds == null || !request.BrandIds.Any())
                {
                    return GenericResponse<bool>.CreateError("Danh sách brand IDs không được trống.");
                }

                var assignedCount = 0;
                var errors = new List<string>();

                foreach (var brandId in request.BrandIds)
                {
                    try
                    {
                        // Kiểm tra quyền sở hữu brand
                        var brand = await _brandRepository.GetByIdAsync(brandId);
                        if (brand == null)
                        {
                            errors.Add($"Không tìm thấy brand với ID: {brandId}");
                            continue;
                        }

                if (brand.ProfileId != existingTeam.ProfileId)
                {
                    return GenericResponse<bool>.CreateError("Brand không thuộc về vendor của team.");
                }
                        if (brand.ProfileId != existingTeam.ProfileId)
                        {
                            errors.Add($"Brand {brand.Name} không thuộc về vendor của team.");
                            continue;
                        }

                        // Kiểm tra brand đã bị xóa mềm chưa
                        if (brand.IsDeleted)
                        {
                            errors.Add($"Không thể assign brand {brand.Name} đã bị xóa.");
                            continue;
                        }

                        // Kiểm tra brand đã được assign cho team chưa
                        var existingAssociation = await _teamBrandRepository.GetByTeamAndBrandAsync(id, brandId);
                        if (existingAssociation != null && existingAssociation.IsActive)
                        {
                            errors.Add($"Brand {brand.Name} đã được assign cho team.");
                            continue;
                        }

                        // Assign brand cho team
                        var teamBrand = new TeamBrand
                        {
                            TeamId = id,
                            BrandId = brandId,
                            IsActive = true
                        };
                        await _teamBrandRepository.AddAsync(teamBrand);
                        assignedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Lỗi khi assign brand {brandId}: {ex.Message}");
                    }
                }

                if (assignedCount == 0)
                {
                    return GenericResponse<bool>.CreateError($"Không thể assign brand nào. Lỗi: {string.Join("; ", errors)}");
                }

                var message = $"Đã assign thành công {assignedCount}/{request.BrandIds.Count} brands cho team.";
                if (errors.Any())
                {
                    message += $" Một số lỗi: {string.Join("; ", errors.Take(3))}";
                }

                return GenericResponse<bool>.CreateSuccess(true, message);
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi assign brands cho team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> UnassignBrandFromTeamAsync(Guid teamId, UnassignBrandFromTeamRequest request, Guid userId)
        {
            try
            {
                // Validate quyền của user với team
                var permissionCheck = await ValidateUserTeamPermission(userId, teamId, "UPDATE_TEAM");
                if (!permissionCheck.Success)
                {
                    return GenericResponse<bool>.CreateError(permissionCheck.Message);
                }

                // Lấy thông tin team hiện tại
                var existingTeam = await _teamRepository.GetByIdAsync(teamId);
                if (existingTeam == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy team.");
                }

                // Kiểm tra team đã bị xóa mềm chưa
                if (existingTeam.IsDeleted)
                {
                    return GenericResponse<bool>.CreateError("Không thể cập nhật brand của team đã bị xóa.");
                }

                // Kiểm tra brand tồn tại
                var brand = await _brandRepository.GetByIdAsync(request.BrandId);
                if (brand == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy brand với ID được cung cấp.");
                }

                // Kiểm tra brand có thuộc về vendor của team không
                if (brand.ProfileId != existingTeam.ProfileId)
                {
                    return GenericResponse<bool>.CreateError("Brand không thuộc về vendor của team.");
                }

                // Tìm association hiện tại
                var existingAssociation = await _teamBrandRepository.GetByTeamAndBrandAsync(teamId, request.BrandId);
                if (existingAssociation == null || !existingAssociation.IsActive)
                {
                    return GenericResponse<bool>.CreateError("Brand chưa được assign cho team.");
                }

                // Unassign brand khỏi team (soft delete)
                existingAssociation.IsActive = false;
                await _teamBrandRepository.UpdateAsync(existingAssociation);

                return GenericResponse<bool>.CreateSuccess(true, $"Đã unassign brand {brand.Name} khỏi team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi unassign brand khỏi team: {ex.Message}");
            }
        }


        public async Task<GenericResponse<bool>> RestoreTeamAsync(Guid id, Guid userId)
        {
            try
            {
                // Validate quyền của user với team
                var permissionCheck = await ValidateUserTeamPermission(userId, id, "UPDATE_TEAM");
                if (!permissionCheck.Success)
                {
                    return GenericResponse<bool>.CreateError(permissionCheck.Message);
                }

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
        
        public async Task<GenericResponse<IEnumerable<TeamResponse>>> GetUserTeamsAsync(Guid userId)
        {
            try
            {
                // Get all team memberships for the user
                var teamMembers = await _teamMemberRepository.GetByUserIdWithBrandsAsync(userId);
                
                if (teamMembers == null || !teamMembers.Any())
                {
                    return GenericResponse<IEnumerable<TeamResponse>>.CreateSuccess(
                        new List<TeamResponse>(), 
                        "Người dùng chưa tham gia team nào");
                }

                // Get team details for each membership
                var teamIds = teamMembers.Select(tm => tm.TeamId).ToList();
                var teams = new List<TeamResponse>();

                foreach (var teamId in teamIds)
                {
                    var team = await _teamRepository.GetByIdAsync(teamId);
                    if (team != null && !team.IsDeleted)
                    {
                        var teamMember = teamMembers.FirstOrDefault(tm => tm.TeamId == teamId);
                        var teamResponse = new TeamResponse
                        {
                            Id = team.Id,
                            ProfileId = team.ProfileId,
                            Name = team.Name,
                            Description = team.Description,
                            CreatedAt = team.CreatedAt,
                            Status = team.Status,
                            MembersCount = team.TeamMembers?.Count(tm => tm.IsActive) ?? 0,
                            UserRole = teamMember?.Role ?? "Member"
                        };
                        teams.Add(teamResponse);
                    }
                }

                return GenericResponse<IEnumerable<TeamResponse>>.CreateSuccess(
                    teams.OrderByDescending(t => t.CreatedAt), 
                    "Lấy danh sách team thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user teams for user {UserId}", userId);
                return GenericResponse<IEnumerable<TeamResponse>>.CreateError("Lỗi khi lấy danh sách team");
            }
        }

        private async Task<List<string>> GetPermissionsByUserId(Guid userId)
        {
            var member = await _teamMemberRepository.GetByUserIdAsync(userId);
            if (member == null || member.Permissions == null || member.Permissions.Count == 0)
                return new List<string> {}; // default permissions
            return member.Permissions;
        }
        
        /// <summary>
        /// Validate quyền của user với team - kiểm tra có phải người tạo team hoặc member có permission
        /// </summary>
        private async Task<GenericResponse<bool>> ValidateUserTeamPermission(Guid userId, Guid teamId, string requiredPermission)
        {
            try
            {
                // Lấy thông tin team hiện tại
                var existingTeam = await _teamRepository.GetByIdAsync(teamId);
                if (existingTeam == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy team.");
                }

                // Kiểm tra quyền: user có phải là người tạo team hay không
                var userProfiles = await _profileRepository.GetByUserIdAsync(userId);
                var proProfiles = userProfiles?.Where(p => p.ProfileType == ProfileTypeEnum.Pro && !p.IsDeleted) ?? new List<Profile>();
                
                // Kiểm tra user có phải là người tạo team (có profile Pro nào thuộc về team này không)
                bool isTeamCreator = proProfiles.Any() && proProfiles.Any(p => p.Id == existingTeam.ProfileId);
                
                if (isTeamCreator)
                {
                    // Người tạo team có full quyền
                    return GenericResponse<bool>.CreateSuccess(true, "User có quyền thực hiện thao tác.");
                }
                else
                {
                    // Nếu không phải người tạo team, kiểm tra có phải member không và có permission không
                    var teamMember = await _teamMemberRepository.GetByTeamAndUserAsync(teamId, userId);
                    if (teamMember == null || !teamMember.IsActive)
                    {
                        return GenericResponse<bool>.CreateError("Bạn không có quyền thực hiện thao tác này.");
                    }

                    var currentUserPermissions = teamMember.Permissions ?? new List<string>();
                    if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, requiredPermission))
                    {
                        return GenericResponse<bool>.CreateError("Bạn không có quyền thực hiện thao tác này.");
                    }

                    return GenericResponse<bool>.CreateSuccess(true, "User có quyền thực hiện thao tác.");
                }
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi validate quyền: {ex.Message}");
            }
        }

        private static TeamResponse MapToResponse(Team team)
        {
            return new TeamResponse
            {
                Id = team.Id,
                ProfileId = team.ProfileId,
                Name = team.Name,
                Description = team.Description,
                CreatedAt = team.CreatedAt,
                Status = team.Status,
                MembersCount = team.TeamMembers?.Count ?? 0
            };
        }

    }
}