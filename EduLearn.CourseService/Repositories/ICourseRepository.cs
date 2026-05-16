using System.Collections.Generic;
using System.Threading.Tasks;
using EduLearn.CourseService.Models;

namespace EduLearn.CourseService.Repositories   
{
    public interface ICourseRepository
    {
        Task<Course?> FindByCourseIdAsync(int courseId);
        Task<IEnumerable<Course>> GetAllAsync();
        Task<IEnumerable<Course>> FindByInstructorIdAsync(int instructorId);
        Task<IEnumerable<Course>> FindByCategoryAsync(string category);
        Task<IEnumerable<Course>> FindByIsPublishedAsync(bool isPublished);
        Task<IEnumerable<Course>> SearchCoursesAsync(string keyword);
        Task<IEnumerable<Course>> FindTopRatedAsync(int count);
        Task AddAsync(Course course);
        Task UpdateAsync(Course course);
        Task DeleteAsync(Course course);
        Task IncrementEnrollmentAsync(int courseId);
        Task DecrementEnrollmentAsync(int courseId);
        Task SaveChangesAsync();
    }
}
