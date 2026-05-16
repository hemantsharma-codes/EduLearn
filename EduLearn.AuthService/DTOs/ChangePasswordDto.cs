using System.ComponentModel.DataAnnotations;

namespace EduLearn.AuthService.DTOs
{
    public class ChangePasswordDto
    {
        [Required]
        public required string OldPassword { get; set; }

        [Required]
        [MinLength(6)]
        public required string NewPassword { get; set; }
    }
}
