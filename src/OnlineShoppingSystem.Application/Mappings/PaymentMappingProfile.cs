using AutoMapper;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Application.Mappings;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Amount.Currency))
            .ForMember(dest => dest.RefundAmount, opt => opt.MapFrom(src => src.RefundAmount != null ? (decimal?)src.RefundAmount.Amount : null));
    }
}