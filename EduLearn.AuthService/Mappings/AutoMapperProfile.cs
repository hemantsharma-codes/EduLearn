using AutoMapper;
using EduLearn.AuthService.DTOs;
using EduLearn.AuthService.Models;

namespace EduLearn.AuthService.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Create mapping between User entity and UserProfileDto
            CreateMap<User, UserProfileDto>();
        }
    }
}
