using EduLearn.ContentService.Models;

namespace EduLearn.ContentService.Repositories
{
    public interface IContentRepository
    {
        // Courses (Validation Replica)
        Task<bool> IsCourseValidAsync(int courseId);

        // Sections
        Task<IEnumerable<Section>> GetSectionsByCourseAsync(int courseId);
        Task<Section?> GetSectionByIdAsync(int sectionId);
        Task<Section> AddSectionAsync(Section section);
        Task UpdateSectionAsync(Section section);
        Task DeleteSectionAsync(int sectionId);

        // Lessons
        Task<IEnumerable<Lesson>> GetLessonsBySectionAsync(int sectionId);
        Task<IEnumerable<Lesson>> GetPreviewLessonsByCourseAsync(int courseId);
        Task<Lesson?> GetLessonByIdAsync(int lessonId);
        Task<Lesson> AddLessonAsync(Lesson lesson);
        Task UpdateLessonAsync(Lesson lesson);
        Task DeleteLessonAsync(int lessonId);
        Task ReorderLessonsAsync(int courseId, IList<int> lessonIds);
        Task<int> GetTotalLessonsAsync(int courseId);
        Task<int> GetCourseIdBySectionIdAsync(int sectionId);
    }
}
