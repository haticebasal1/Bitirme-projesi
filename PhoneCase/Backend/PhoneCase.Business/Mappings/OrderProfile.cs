using System;
using AutoMapper;
using PhoneCase.Entities.Concrete;
using PhoneCase.Shared.Dtos.OrderDtos;

namespace PhoneCase.Business.Mappings
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

            CreateMap<Order, OrderDto>()
                .ForMember(
                    dest => dest.OrderDate,
                    opt => opt.MapFrom(src => TimeZoneInfo.ConvertTime(src.CreatedAt.UtcDateTime, turkeyTimeZone)))
                .ForMember(
                    dest => dest.CanceledDate,
                    opt => opt.MapFrom(src => TimeZoneInfo.ConvertTime(src.DeletedAt.UtcDateTime, turkeyTimeZone)))
                .ForMember(
                    dest => dest.OrderStatusUpdatedDate,
                    opt => opt.MapFrom(src => TimeZoneInfo.ConvertTime(src.UpdatedAt.UtcDateTime, turkeyTimeZone)))
                .ForMember(
                    dest => dest.OrderItems,
                    opt => opt.MapFrom(src => src.OrderItems))
                .ReverseMap();

            // 🔧 DÜZENLENEN KISIM SADECE BURASI
            CreateMap<OrderNowDto, Order>()
                .ForMember(
                    dest => dest.OrderItems,
                    opt => opt.MapFrom(src => src.OrderItems.ToList()));

            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(
                    dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product!.Name))
                .ForMember(
                    dest => dest.ProductImageUrl,
                    opt => opt.MapFrom(src => src.Product!.ImageUrl));

            CreateMap<OrderItemDto, OrderItem>();
            CreateMap<OrderItemCreateDto, OrderItem>();
        }
    }
}