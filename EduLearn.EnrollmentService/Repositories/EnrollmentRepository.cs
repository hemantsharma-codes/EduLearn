using EduLearn.EnrollmentService.Data;
using EduLearn.EnrollmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.EnrollmentService.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly EnrollmentDbContext _context;

        public EnrollmentRepository(EnrollmentDbContext context)
        {
            _context = context;
        }

        public async Task<Enrollment> AddEnrollmentAsync(Enrollment enrollment)
        {
            await _context.Enrollments.AddAsync(enrollment);
            await _context.SaveChangesAsync();
            return enrollment;
        }

        public async Task<Enrollment?> FindByEnrollmentIdAsync(int id)
        {
            return await _context.Enrollments.FindAsync(id);
        }

        public async Task<IEnumerable<Enrollment>> FindByStudentIdAsync(int studentId)
        {
            return await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Enrollment>> FindByCourseIdAsync(int courseId)
        {
            return await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.CourseId == courseId)
                .ToListAsync();
        }

        public async Task<Enrollment?> FindByStudentAndCourseAsync(int studentId, int courseId)
        {
            return await _context.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        }

        public async Task<bool> IsEnrolledAsync(int studentId, int courseId)
        {
            return await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.Status != "DROPPED");
        }

        public async Task<int> CountByCourseIdAsync(int courseId)
        {
            return await _context.Enrollments
                .CountAsync(e => e.CourseId == courseId && e.Status != "DROPPED");
        }

        public async Task UpdateEnrollmentAsync(Enrollment enrollment)
        {
            _context.Enrollments.Update(enrollment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteEnrollmentsByStudentAsync(int studentId)
        {
            var enrollments = await _context.Enrollments.Where(e => e.StudentId == studentId).ToListAsync();
            if (enrollments.Any())
            {
                _context.Enrollments.RemoveRange(enrollments);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string?> GetCourseTitleAsync(int courseId)
        {
            var course = await _context.CourseRefs.FindAsync(courseId);
            return course?.Title;
        }
    }
}
