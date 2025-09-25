using AutoMapper;
using AISAM.Data.Model;
using AISAM.API.DTO.Request;
using AISAM.API.DTO.Response;

namespace AISAM.API.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapping: API DTO CreateUserRequestDto -> Data Model User
            CreateMap<CreateUserRequestDto, User>()
                .ForMember(d => d.Id, o => o.MapFrom(s => Guid.NewGuid()))
                .ForMember(d => d.Email, o => o.MapFrom(s => s.Email))
                .ForMember(d => d.PasswordHash, o => o.MapFrom(s => s.Password)) // Map password to PasswordHash for hashing
                .ForMember(d => d.IsActive, o => o.MapFrom(s => true))
                .ForMember(d => d.isBanned, o => o.MapFrom(s => false))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => DateTime.UtcNow))
                .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => DateTime.UtcNow))
                .ForMember(d => d.LastLoginAt, o => o.Ignore())
                .ForMember(d => d.MfaEnabled, o => o.MapFrom(s => false))
                .ForMember(d => d.Role, o => o.MapFrom(s => "user"))
                .ForMember(d => d.SocialAccounts, o => o.Ignore())
                .ForMember(d => d.Posts, o => o.Ignore());
            
            // Mapping: Data Model User -> Common Models UserResponseDto
            CreateMap<User, AISAM.Common.Models.UserResponseDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Email, o => o.MapFrom(s => s.Email ?? string.Empty))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.SocialAccounts, o => o.Ignore());
        }
    }
}


