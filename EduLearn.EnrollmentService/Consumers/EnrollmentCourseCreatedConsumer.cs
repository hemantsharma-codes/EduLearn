using EduLearn.EnrollmentService.Data;
using EduLearn.EnrollmentService.Models;
using EduLearn.SharedLib.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.EnrollmentService.Consumers
{
    public class EnrollmentCourseCreatedConsumer : IConsumer<ICourseCreatedEvent>
    {
        private readonly EnrollmentDbContext _db;
        private readonly ILogger<EnrollmentCourseCreatedConsumer> _logger;

        public EnrollmentCourseCreatedConsumer(EnrollmentDbContext db, ILogger<EnrollmentCourseCreatedConsumer> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ICourseCreatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("[ENROLLMENT-SYNC] Syncing CourseId {CourseId} title: {Title}, thumbnail: {Thumb}", msg.CourseId, msg.Title, msg.ThumbnailUrl);

            var course = await _db.CourseRefs.FindAsync(msg.CourseId);
            if (course == null)
            {
                _db.CourseRefs.Add(new CourseRef 
                { 
                    CourseId = msg.CourseId, 
                    Title = msg.Title,
                    ThumbnailUrl = msg.ThumbnailUrl 
                });
            }
            else
            {
                course.Title = msg.Title;
                course.ThumbnailUrl = msg.ThumbnailUrl;
                course.LastUpdated = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }
    }
}
