using AutoMapper;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Application.Mappings;

public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        CreateMap<Cart, CartDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.GetTotal().Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.GetTotal().Currency))
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.GetTotalItemCount()));

        CreateMap<CartItem, CartItemDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency))
            .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.GetSubtotal().Amount));
    }
}