using System.ComponentModel.DataAnnotations;

namespace EduLearn.ContentService.DTOs
{
    public class CreateLessonDto
    {
        [Required]
        public int SectionId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public required string Title { get; set; }

        public string? ContentUrl { get; set; }

        [Required]
        public string ContentType { get; set; } = "VIDEO";

        [Range(0, 86400)] // Allow 0 for non-video content
        public int? Duration { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPreview { get; set; }
    }
}
