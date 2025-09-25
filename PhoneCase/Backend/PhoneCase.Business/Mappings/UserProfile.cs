using System;
using AutoMapper;
using PhoneCase.Entities.Concrete;
using PhoneCase.Shared.Dtos.AuthDtos;

namespace PhoneCase.Business.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
     }
}
