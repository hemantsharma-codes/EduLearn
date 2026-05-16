using AutoMapper;
using EduLearn.EnrollmentService.DTOs;
using EduLearn.EnrollmentService.Models;

namespace EduLearn.EnrollmentService.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Enrollment, EnrollmentResponseDto>()
                .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src => src.Course != null ? src.Course.Title : null))
                .ForMember(dest => dest.CourseThumbnail, opt => opt.MapFrom(src => src.Course != null ? src.Course.ThumbnailUrl : null));
        }
    }
}
