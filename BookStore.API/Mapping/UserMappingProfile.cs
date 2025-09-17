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
                // Để demo selective mapping, comment các fields không muốn map:
                // .ForMember(dest => dest.FullName, opt => opt.Ignore())
                // .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
                // .ForMember(dest => dest.Address, opt => opt.Ignore())
                // .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                // .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())

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
