using System.ComponentModel.DataAnnotations;

namespace EduLearn.ContentService.DTOs
{
    public class CreateSectionDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public required string Title { get; set; }

        public int DisplayOrder { get; set; }
    }
}
