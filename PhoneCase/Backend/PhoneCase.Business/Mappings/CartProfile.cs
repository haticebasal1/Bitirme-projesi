using System;
using AutoMapper;
using PhoneCase.Entities.Concrete;
using PhoneCase.Shared.Dtos.CartDtos;
using PhoneCase.Shared.Dtos.ProductDtos;

namespace PhoneCase.Business.Mappings;

public class CartProfile : Profile
{
    public CartProfile()
    {
        CreateMap<Cart, CartDto>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
            .ForMember(dest => dest.CartItems, opt => opt.MapFrom(src => src.CartItems))
            .ReverseMap();

        CreateMap<CartItem, CartItemDto>()
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))  // Buraya ekledik
            .ReverseMap();

        CreateMap<CartCreateDto, Cart>();
    }
}
