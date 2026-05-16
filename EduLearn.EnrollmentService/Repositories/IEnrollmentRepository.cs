using EduLearn.EnrollmentService.Models;

namespace EduLearn.EnrollmentService.Repositories
{
    public interface IEnrollmentRepository
    {
        Task<Enrollment> AddEnrollmentAsync(Enrollment enrollment);
        Task<Enrollment?> FindByEnrollmentIdAsync(int id);
        Task<IEnumerable<Enrollment>> FindByStudentIdAsync(int studentId);
        Task<IEnumerable<Enrollment>> FindByCourseIdAsync(int courseId);
        Task<Enrollment?> FindByStudentAndCourseAsync(int studentId, int courseId);
        Task<bool> IsEnrolledAsync(int studentId, int courseId);
        Task<int> CountByCourseIdAsync(int courseId);
        Task UpdateEnrollmentAsync(Enrollment enrollment);
        Task DeleteEnrollmentsByStudentAsync(int studentId);
        Task<string?> GetCourseTitleAsync(int courseId);
    }
}
