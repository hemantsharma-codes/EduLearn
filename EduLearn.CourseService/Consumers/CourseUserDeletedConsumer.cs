using EduLearn.CourseService.Services;
using EduLearn.SharedLib.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace EduLearn.CourseService.Consumers
{
    public class CourseUserDeletedConsumer : IConsumer<IUserDeletedEvent>
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CourseUserDeletedConsumer> _logger;

        public CourseUserDeletedConsumer(ICourseService courseService, ILogger<CourseUserDeletedConsumer> logger)
        {
            _courseService = courseService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IUserDeletedEvent> context)
        {
            var userId = context.Message.UserId;
            var role = context.Message.Role;

            _logger.LogInformation("Message received: User {UserId} with Role {Role} deleted. Processing related data...", userId, role);

            // We only care if the deleted user is an INSTRUCTOR (since students don't own courses)
            if (role == "INSTRUCTOR" || role == "ADMIN")
            {
                var instructorCourses = await _courseService.GetCoursesByInstructorAsync(userId);
                foreach (var course in instructorCourses)
                {
                    // By passing 0 as the instructorId to DeleteCourseAsync, we act as an admin/system
                    // This will also publish CourseDeletedEvent to ContentService
                    await _courseService.DeleteCourseAsync(course.CourseId, 0);
                    _logger.LogInformation("Cascade Delete: Deleted Course {CourseId} owned by User {UserId}", course.CourseId, userId);
                }
            }
        }
    }
}
