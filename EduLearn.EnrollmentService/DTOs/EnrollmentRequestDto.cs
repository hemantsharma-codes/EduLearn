using System.ComponentModel.DataAnnotations;

namespace EduLearn.EnrollmentService.DTOs
{
    public class EnrollmentRequestDto
    {
        [Required(ErrorMessage = "CourseId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "CourseId must be a valid positive integer.")]
        public int CourseId { get; set; }

        public string? PaymentId { get; set; }
    }
}
