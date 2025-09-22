using AutoMapper;
using BookStore.Data.Model;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;

namespace BookStore.API.Mapping
{
    public class BookStoreMappingProfile : Profile
    {
        public BookStoreMappingProfile()
        {
            // User mappings
            CreateMap<User, UserResponseDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<CreateUserRequestDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => UserStatusEnum.Active));

            // Author mappings
            CreateMap<Author, AuthorResponseDto>();
            CreateMap<CreateAuthorRequestDto, Author>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<UpdateAuthorRequestDto, Author>();

            // Publisher mappings
            CreateMap<Publisher, PublisherResponseDto>();
            CreateMap<CreatePublisherRequestDto, Publisher>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<UpdatePublisherRequestDto, Publisher>();
        }
    }
}
