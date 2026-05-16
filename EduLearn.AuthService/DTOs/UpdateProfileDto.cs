using System.ComponentModel.DataAnnotations;

namespace EduLearn.AuthService.DTOs
{
    public class UpdateProfileDto
    {
        [Required]
        [MaxLength(100)]
        public required string FullName { get; set; }
    }
}
