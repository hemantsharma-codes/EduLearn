using Microsoft.EntityFrameworkCore;
using EduLearn.ContentService.Models;

namespace EduLearn.ContentService.Data
{
    public class ContentDbContext : DbContext
    {
        public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
        {
        }

        public DbSet<Section> Sections { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<ValidCourse> ValidCourses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure one-to-many relationship between Section and Lesson
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Section)
                .WithMany(s => s.Lessons)
                .HasForeignKey(l => l.SectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed some data or additional configurations if needed
        }
    }
}
