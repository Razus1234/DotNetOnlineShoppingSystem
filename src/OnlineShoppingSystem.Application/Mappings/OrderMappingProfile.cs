using AutoMapper;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Application.Mappings;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Total.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Total.Currency))
            .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => src.ShippingAddress))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.Payment, opt => opt.MapFrom(src => src.Payment));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency))
            .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.GetSubtotal().Amount));
    }
}