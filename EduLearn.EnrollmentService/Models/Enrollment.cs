using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.EnrollmentService.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, COMPLETED, DROPPED
        public int ProgressPercent { get; set; } = 0;
        public DateTime? LastAccessedAt { get; set; }
        public bool CertificateIssued { get; set; } = false;
        public string? PaymentId { get; set; }

        [ForeignKey("CourseId")]
        public virtual CourseRef? Course { get; set; }
    }
}
