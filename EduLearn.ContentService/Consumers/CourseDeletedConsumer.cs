using EduLearn.ContentService.Services;
using EduLearn.SharedLib.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EduLearn.ContentService.Consumers
{
    public class CourseDeletedConsumer : IConsumer<ICourseDeletedEvent>
    {
        private readonly IContentService _contentService;
        private readonly EduLearn.ContentService.Data.ContentDbContext _dbContext;
        private readonly ILogger<CourseDeletedConsumer> _logger;

        public CourseDeletedConsumer(IContentService contentService, EduLearn.ContentService.Data.ContentDbContext dbContext, ILogger<CourseDeletedConsumer> logger)
        {
            _contentService = contentService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ICourseDeletedEvent> context)
        {
            var courseId = context.Message.CourseId;
            _logger.LogInformation("Message received: Course {CourseId} deleted. Cleaning up content...", courseId);

            try
            {
                var sections = await _contentService.GetSectionsByCourseAsync(courseId);

                foreach (var section in sections)
                {
                    await _contentService.DeleteSectionAsync(section.SectionId);
                }

                var validCourse = await _dbContext.ValidCourses.FindAsync(courseId);
                if (validCourse != null)
                {
                    _dbContext.ValidCourses.Remove(validCourse);
                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully cleaned up content for Course {CourseId}", courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning up content for Course {CourseId}", courseId);
                throw; 
            }
        }
    }
}
