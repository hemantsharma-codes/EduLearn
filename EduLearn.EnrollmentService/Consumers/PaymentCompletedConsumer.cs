using EduLearn.EnrollmentService.Data;
using EduLearn.EnrollmentService.Models;
using EduLearn.SharedLib.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.EnrollmentService.Consumers
{
    public class PaymentCompletedConsumer : IConsumer<IPaymentCompletedEvent>
    {
        private readonly EnrollmentDbContext _db;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<PaymentCompletedConsumer> _logger;

        public PaymentCompletedConsumer(
            EnrollmentDbContext db, 
            IPublishEndpoint publishEndpoint,
            ILogger<PaymentCompletedConsumer> logger)
        {
            _db = db;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IPaymentCompletedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("[ENROLLMENT-AUTO] RECEIVED PaymentCompletedEvent: Course {CourseId}, User {UserId}, Payment {PaymentId}", msg.CourseId, msg.UserId, msg.PaymentId);

            try
            {
                // 1. Process Enrollment in DB
                var existing = await _db.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == msg.UserId && e.CourseId == msg.CourseId);

                int enrollmentId;
                if (existing != null)
                {
                    _logger.LogInformation("[ENROLLMENT-AUTO] Found existing enrollment for User {UserId} in Course {CourseId}. Status: {Status}", msg.UserId, msg.CourseId, existing.Status);
                    if (existing.Status == "DROPPED")
                    {
                        existing.Status = "ACTIVE";
                        existing.PaymentId = msg.PaymentId;
                        existing.EnrolledAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                    enrollmentId = existing.EnrollmentId;
                }
                else
                {
                    _logger.LogInformation("[ENROLLMENT-AUTO] Creating NEW enrollment for User {UserId} in Course {CourseId}", msg.UserId, msg.CourseId);
                    var enrollment = new Enrollment
                    {
                        StudentId = msg.UserId,
                        CourseId = msg.CourseId,
                        PaymentId = msg.PaymentId,
                        Status = "ACTIVE",
                        EnrolledAt = DateTime.UtcNow
                    };
                    _db.Enrollments.Add(enrollment);
                    await _db.SaveChangesAsync(); // Save to get the ID
                    enrollmentId = enrollment.EnrollmentId;
                }

                _logger.LogInformation("[ENROLLMENT-AUTO] SUCCESS: DB Enrollment {Id} is now ACTIVE", enrollmentId);

                // 2. Fetch Course Title for the Event
                var courseRef = await _db.CourseRefs.FindAsync(msg.CourseId);
                if (courseRef == null) {
                    _logger.LogWarning("[ENROLLMENT-AUTO] CourseRef {Id} MISSING in Enrollment DB! Fallback title used.", msg.CourseId);
                }
                string courseTitle = courseRef?.Title ?? $"Course #{msg.CourseId}";

                // 3. Publish Event for NotificationService
                _logger.LogInformation("[ENROLLMENT-AUTO] Publishing IEnrollmentCreatedEvent...");
                await _publishEndpoint.Publish<IEnrollmentCreatedEvent>(new
                {
                    EnrollmentId = enrollmentId,
                    StudentId = msg.UserId,
                    CourseId = msg.CourseId,
                    CourseTitle = courseTitle,
                    StudentName = msg.UserName,
                    StudentEmail = msg.UserEmail,
                    EnrolledAt = DateTime.UtcNow
                });

                _logger.LogInformation("[ENROLLMENT-AUTO] COMPLETED: Enrollment process finished for User {UserId}", msg.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ENROLLMENT-AUTO] FATAL ERROR processing payment for Course {CourseId}, User {UserId}", msg.CourseId, msg.UserId);
                throw; // MassTransit will retry
            }
        }
    }
}
