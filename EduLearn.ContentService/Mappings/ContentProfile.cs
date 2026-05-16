using AutoMapper;
using EduLearn.ContentService.DTOs;
using EduLearn.ContentService.Models;

namespace EduLearn.ContentService.Mappings
{
    public class ContentProfile : Profile
    {
        public ContentProfile()
        {
            CreateMap<Section, SectionResponseDto>();
            CreateMap<CreateSectionDto, Section>();

            CreateMap<Lesson, LessonResponseDto>()
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType.ToString()));
            
            CreateMap<CreateLessonDto, Lesson>()
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => Enum.Parse<ContentType>(src.ContentType, true)));
        }
    }
}
