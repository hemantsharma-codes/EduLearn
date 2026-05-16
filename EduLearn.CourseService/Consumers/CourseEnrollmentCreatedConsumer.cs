using EduLearn.CourseService.Services;
using EduLearn.SharedLib.Messaging;
using MassTransit;

namespace EduLearn.CourseService.Consumers
{
    public class CourseEnrollmentCreatedConsumer : IConsumer<IEnrollmentCreatedEvent>
    {
        private readonly ICourseService _courseService;

        public CourseEnrollmentCreatedConsumer(ICourseService courseService)
        {
            _courseService = courseService;
        }

        public async Task Consume(ConsumeContext<IEnrollmentCreatedEvent> context)
        {
            var courseId = context.Message.CourseId;
            
            // Increment the enrollment count for the course
            await _courseService.IncrementEnrollmentAsync(courseId);
        }
    }
}
