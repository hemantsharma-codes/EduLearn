using MassTransit;
using EduLearn.SharedLib.Messaging;
using EduLearn.ContentService.Data;
using EduLearn.ContentService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduLearn.ContentService.Consumers
{
    public class CourseCreatedConsumer : IConsumer<ICourseCreatedEvent>
    {
        private readonly ContentDbContext _dbContext;
        private readonly ILogger<CourseCreatedConsumer> _logger;

        public CourseCreatedConsumer(ContentDbContext dbContext, ILogger<CourseCreatedConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ICourseCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation($"Received CourseCreatedEvent for CourseId: {message.CourseId}");

            // Check if it already exists to be safe
            var exists = await _dbContext.ValidCourses.AnyAsync(c => c.CourseId == message.CourseId);
            
            if (!exists)
            {
                var validCourse = new ValidCourse
                {
                    CourseId = message.CourseId,
                    Title = message.Title
                };

                await _dbContext.ValidCourses.AddAsync(validCourse);
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation($"Successfully added CourseId: {message.CourseId} to local ValidCourses replica.");
            }
        }
    }
}
