using AutoMapper;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Application.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses));

        CreateMap<Address, AddressDto>();
        CreateMap<AddressDto, Address>();
    }
}