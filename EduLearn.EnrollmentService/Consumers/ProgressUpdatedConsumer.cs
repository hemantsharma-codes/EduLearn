using System;
using System.Threading.Tasks;
using EduLearn.EnrollmentService.Repositories;
using EduLearn.SharedLib.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EduLearn.EnrollmentService.Consumers
{
    public class ProgressUpdatedConsumer : IConsumer<IProgressUpdatedEvent>
    {
        private readonly IEnrollmentRepository _repository;
        private readonly ILogger<ProgressUpdatedConsumer> _logger;

        public ProgressUpdatedConsumer(IEnrollmentRepository repository, ILogger<ProgressUpdatedConsumer> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IProgressUpdatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Updating progress for Student {StudentId} in Course {CourseId} to {ProgressPercent}%", 
                msg.StudentId, msg.CourseId, msg.ProgressPercent);

            var enrollment = await _repository.FindByStudentAndCourseAsync(msg.StudentId, msg.CourseId);
            if (enrollment != null)
            {
                enrollment.ProgressPercent = msg.ProgressPercent;
                await _repository.UpdateEnrollmentAsync(enrollment);
            }
        }
    }
}
