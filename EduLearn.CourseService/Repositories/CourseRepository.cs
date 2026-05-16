using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduLearn.CourseService.Models;
using EduLearn.CourseService.Data;

namespace EduLearn.CourseService.Repositories
{
    // repository implementation for Course data access using Entity Framework Core
    public class CourseRepository : ICourseRepository
    {
        private readonly CourseDbContext _context;

        public CourseRepository(CourseDbContext context)
        {
            _context = context;
        }

        public async Task<Course?> FindByCourseIdAsync(int courseId)
        {
            return await _context.Courses.FindAsync(courseId);
        }

        public async Task<IEnumerable<Course>> GetAllAsync()
        {
            return await _context.Courses.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Course>> FindByInstructorIdAsync(int instructorId)
        {
            return await _context.Courses
                .AsNoTracking()
                .IgnoreQueryFilters() 
                .Where(c => c.InstructorId == instructorId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Course>> FindByCategoryAsync(string category)
        {
            return await _context.Courses
                .AsNoTracking()
                .Where(c => c.Category.ToLower() == category.ToLower())
                .ToListAsync();
        }

        public async Task<IEnumerable<Course>> FindByIsPublishedAsync(bool isPublished)
        {
            return await _context.Courses
                .AsNoTracking()
                .Where(c => c.IsPublished == isPublished)
                .ToListAsync();
        }

        public async Task<IEnumerable<Course>> SearchCoursesAsync(string keyword)
        {
            var lowerKeyword = keyword.ToLower();
            return await _context.Courses
                .AsNoTracking()
                .Where(c => c.Title.ToLower().Contains(lowerKeyword) || c.Description.ToLower().Contains(lowerKeyword))
                .ToListAsync();
        }

        public async Task<IEnumerable<Course>> FindTopRatedAsync(int count)
        {
            // Note: In a production scenario, we would join with the Reviews table to calculate Avg Rating
            return await _context.Courses
                .AsNoTracking()
                .Where(c => c.IsPublished && c.IsApproved)
                .OrderByDescending(c => c.EnrollmentCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task AddAsync(Course course)
        {
            await _context.Courses.AddAsync(course);
        }

        public Task UpdateAsync(Course course)
        {
            _context.Courses.Update(course);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Course course)
        {
            _context.Courses.Remove(course);
            return Task.CompletedTask;
        }

        public async Task IncrementEnrollmentAsync(int courseId)
        {
            await _context.Courses
                .Where(c => c.CourseId == courseId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(c => c.EnrollmentCount, c => c.EnrollmentCount + 1));
        }

        public async Task DecrementEnrollmentAsync(int courseId)
        {
            await _context.Courses
                .Where(c => c.CourseId == courseId && c.EnrollmentCount > 0)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(c => c.EnrollmentCount, c => c.EnrollmentCount - 1));
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

