using EduLearn.EnrollmentService.Services;
using EduLearn.SharedLib.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace EduLearn.EnrollmentService.Consumers
{
    public class EnrollmentUserDeletedConsumer : IConsumer<IUserDeletedEvent>
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ILogger<EnrollmentUserDeletedConsumer> _logger;

        public EnrollmentUserDeletedConsumer(IEnrollmentService enrollmentService, ILogger<EnrollmentUserDeletedConsumer> logger)
        {
            _enrollmentService = enrollmentService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IUserDeletedEvent> context)
        {
            var userId = context.Message.UserId;
            var role = context.Message.Role;

            _logger.LogInformation("Message received: User {UserId} with Role {Role} deleted. Processing related data...", userId, role);

            // We only care if the deleted user is a STUDENT
            if (string.Equals(role, "STUDENT", System.StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Hard Delete: Removing all enrollments for deleted Student {UserId}", userId);
                await _enrollmentService.DeleteAllEnrollmentsByStudentAsync(userId);
            }
        }
    }
}
