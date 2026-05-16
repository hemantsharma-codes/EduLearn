using EduLearn.EnrollmentService.Data;
using EduLearn.SharedLib.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.EnrollmentService.Consumers
{
    public class EnrollmentCourseDeletedConsumer : IConsumer<ICourseDeletedEvent>
    {
        private readonly EnrollmentDbContext _db;
        private readonly ILogger<EnrollmentCourseDeletedConsumer> _logger;

        public EnrollmentCourseDeletedConsumer(EnrollmentDbContext db, ILogger<EnrollmentCourseDeletedConsumer> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ICourseDeletedEvent> context)
        {
            var courseId = context.Message.CourseId;
            _logger.LogInformation("[ENROLLMENT-SYNC] RECEIVED CourseDeletedEvent for Course {Id}. Cleaning up...", courseId);

            // 1. Remove all enrollments for this course
            var enrollments = await _db.Enrollments.Where(e => e.CourseId == courseId).ToListAsync();
            if (enrollments.Any())
            {
                _db.Enrollments.RemoveRange(enrollments);
                _logger.LogInformation("[ENROLLMENT-SYNC] Removed {Count} enrollments for deleted course {Id}", enrollments.Count, courseId);
            }

            // 2. Remove the course reference
            var courseRef = await _db.CourseRefs.FindAsync(courseId);
            if (courseRef != null)
            {
                _db.CourseRefs.Remove(courseRef);
                _logger.LogInformation("[ENROLLMENT-SYNC] Removed CourseRef for {Id}", courseId);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("[ENROLLMENT-SYNC] Cleanup completed for Course {Id}", courseId);
        }
    }
}
