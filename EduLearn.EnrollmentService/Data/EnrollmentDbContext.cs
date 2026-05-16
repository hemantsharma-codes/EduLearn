using EduLearn.EnrollmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.EnrollmentService.Data
{
    public class EnrollmentDbContext : DbContext
    {
        public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : base(options) { }

        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<CourseRef> CourseRefs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prevent duplicate enrollments
            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentId, e.CourseId })
                .IsUnique();
        }
    }
}
