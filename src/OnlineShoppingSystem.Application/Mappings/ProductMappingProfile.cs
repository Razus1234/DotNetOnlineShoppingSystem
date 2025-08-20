using AutoMapper;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Application.Mappings;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency))
            .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => src.ImageUrls));
    }
}