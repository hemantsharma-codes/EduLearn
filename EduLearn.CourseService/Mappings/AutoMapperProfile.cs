using AutoMapper;
using EduLearn.CourseService.Models;
using EduLearn.CourseService.DTOs;

namespace EduLearn.CourseService.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Course, CourseResponseDto>();
            CreateMap<CreateCourseRequestDto, Course>();
        }
    }
}
    