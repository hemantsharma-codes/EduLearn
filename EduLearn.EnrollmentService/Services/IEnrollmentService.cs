using EduLearn.EnrollmentService.DTOs;

namespace EduLearn.EnrollmentService.Services
{
    public interface IEnrollmentService
    {
        Task<EnrollmentResponseDto> EnrollAsync(int studentId, string studentEmail, string studentName, EnrollmentRequestDto requestDto);
        Task<EnrollmentResponseDto> GetEnrollmentByIdAsync(int enrollmentId);
        Task<IEnumerable<EnrollmentResponseDto>> GetEnrollmentsByStudentAsync(int studentId);
        Task<IEnumerable<EnrollmentResponseDto>> GetEnrollmentsByCourseAsync(int courseId);
        Task<bool> IsEnrolledAsync(int studentId, int courseId);
        Task DropCourseAsync(int studentId, int courseId);
        Task<int> GetEnrollmentCountAsync(int courseId);
        Task DeleteAllEnrollmentsByStudentAsync(int studentId);
    }
}
