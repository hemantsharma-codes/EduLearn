using System.ComponentModel.DataAnnotations;

namespace EduLearn.AuthService.DTOs
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}
