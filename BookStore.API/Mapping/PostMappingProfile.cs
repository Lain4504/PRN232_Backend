using AutoMapper;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;
using BookStore.Common.Enumeration;
using BookStore.Data.Model;

namespace BookStore.API.Mapping
{
    public class PostMappingProfile : Profile
    {
        public PostMappingProfile()
        {
            CreateMap<Post, PostResponseDto>()
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author.FullName))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<CreatePostRequestDto, Post>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignore vì sẽ được tạo tự động
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Sẽ được set từ JWT token
                .ForMember(dest => dest.Author, opt => opt.Ignore()); // Navigation property sẽ được load riêng
        }
    }
}
