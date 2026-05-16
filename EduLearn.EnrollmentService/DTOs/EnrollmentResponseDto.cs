using System;

namespace EduLearn.EnrollmentService.DTOs
{
    public class EnrollmentResponseDto
    {
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
        public string? PaymentId { get; set; }
        public string? CourseTitle { get; set; }
        public string? CourseThumbnail { get; set; }
    }
}
