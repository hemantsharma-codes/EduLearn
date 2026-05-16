using EduLearn.ContentService.DTOs;

namespace EduLearn.ContentService.Services
{
    public interface IContentService
    {
        Task<IEnumerable<SectionResponseDto>> GetSectionsByCourseAsync(int courseId);
        Task<SectionResponseDto> CreateSectionAsync(CreateSectionDto dto);
        Task UpdateSectionAsync(int id, CreateSectionDto dto);
        Task DeleteSectionAsync(int id);

        Task<IEnumerable<LessonResponseDto>> GetLessonsBySectionAsync(int sectionId);
        Task<IEnumerable<LessonResponseDto>> GetPreviewLessonsByCourseAsync(int courseId);
        Task<LessonResponseDto?> GetLessonByIdAsync(int id);
        Task<LessonResponseDto> CreateLessonAsync(CreateLessonDto dto, IFormFile? file);
        Task UpdateLessonAsync(int id, CreateLessonDto dto);
        Task DeleteLessonAsync(int id);
        Task ReorderLessonsAsync(int courseId, IList<int> lessonIds);
    }
}
