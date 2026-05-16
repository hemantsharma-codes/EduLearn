using Microsoft.EntityFrameworkCore;
using EduLearn.CourseService.Models;

namespace EduLearn.CourseService.Data
{
    public class CourseDbContext : DbContext
    {
        public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options) { }

        public DbSet<Course> Courses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
