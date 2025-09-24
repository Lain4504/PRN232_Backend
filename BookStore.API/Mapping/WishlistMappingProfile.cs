
using AutoMapper;
using BookStore.Data.Model;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;

namespace BookStore.API.Mapping
{
    public class WishlistMappingProfile : Profile
    {
        public WishlistMappingProfile()
        {
            // Mapping from Wishlist entity to WishlistResponseDto
            CreateMap<Wishlist, WishlistResponseDto>()
                .ForMember(dest => dest.Book, opt => opt.MapFrom(src => src.Book))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            // Mapping from CreateWishlistRequestDto to Wishlist entity
            CreateMap<CreateWishlistRequestDto, Wishlist>()
                .ForMember(dest => dest.Book, opt => opt.Ignore()) // Book sẽ được set ở Service/Repository
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        }
    }
}
