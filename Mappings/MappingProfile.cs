using AutoMapper;
using HomevoAPI.Models;
using HomevoAPI.DTOs;

namespace HomevoAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : ""));

            CreateMap<CreateProductDto, Product>();
        }
    }
}
