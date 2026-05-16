using System.Collections.Generic;
using System.Threading.Tasks;
using EduLearn.CourseService.DTOs;

namespace EduLearn.CourseService.Services
{
    // service for managing course lifecycle, search, and administrative operations
    public interface ICourseService
    {
        // creates a new course draft
        Task<CourseResponseDto> CreateCourseAsync(int instructorId, string instructorName, CreateCourseRequestDto dto, IFormFile? thumbnail);
        Task<IEnumerable<CourseResponseDto>> GetAllCoursesAsync();

        // get a course by its unique identifier
        Task<CourseResponseDto?> GetCourseByIdAsync(int courseId);

        // get all courses created by a specific instructor
        Task<IEnumerable<CourseResponseDto>> GetCoursesByInstructorAsync(int instructorId);

        // get approved and published courses within a specific category
        Task<IEnumerable<CourseResponseDto>> GetCoursesByCategoryAsync(string category);

        // get all courses that are currently published and approved
        Task<IEnumerable<CourseResponseDto>> GetPublishedCoursesAsync();

        // searches for approved courses matching a specific keyword in title or description
        Task<IEnumerable<CourseResponseDto>> SearchCoursesAsync(string keyword);

        // update an existing course. This will reset the approval and publication status
        Task<bool> UpdateCourseAsync(int courseId, int instructorId, UpdateCourseRequestDto dto);

        // marks a course as published, making it visible for administrative approval
        Task<bool> PublishCourseAsync(int courseId, int instructorId);

        // administratively approves a course for public visibility
        Task<bool> ApproveCourseAsync(int courseId);

        // delete a course
        Task<bool> DeleteCourseAsync(int courseId, int instructorId);

        // get the top-rated or most popular courses
        Task<IEnumerable<CourseResponseDto>> GetTopRatedCoursesAsync(int count);

        // increments the enrollment count for a course
        Task IncrementEnrollmentAsync(int courseId);

        // decrements the enrollment count for a course
        Task DecrementEnrollmentAsync(int courseId);
    }
}

