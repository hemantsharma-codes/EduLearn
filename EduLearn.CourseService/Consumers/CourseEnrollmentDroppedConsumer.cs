using EduLearn.CourseService.Services;
using EduLearn.SharedLib.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace EduLearn.CourseService.Consumers
{
    public class CourseEnrollmentDroppedConsumer : IConsumer<IEnrollmentDroppedEvent>
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CourseEnrollmentDroppedConsumer> _logger;

        public CourseEnrollmentDroppedConsumer(ICourseService courseService, ILogger<CourseEnrollmentDroppedConsumer> logger)
        {
            _courseService = courseService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IEnrollmentDroppedEvent> context)
        {
            var courseId = context.Message.CourseId;
            _logger.LogInformation("Student {StudentId} dropped Course {CourseId}. Decrementing enrollment count.", context.Message.StudentId, courseId);
            
            await _courseService.DecrementEnrollmentAsync(courseId);
        }
    }
}
