using AutoMapper;
using BookStore.Data.Model;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;

namespace BookStore.API.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            // Mapping từ User entity sang UserResponseDto (full mapping)
            CreateMap<User, UserResponseDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            // Mapping từ User entity sang UserSelectiveResponseDto (selective mapping - chỉ một số fields)
            CreateMap<User, UserSelectiveResponseDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            // Mapping từ CreateUserRequestDto sang User entity
            CreateMap<CreateUserRequestDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignore vì sẽ được tạo tự động
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => UserStatusEnum.Active));
        }
    }
}
