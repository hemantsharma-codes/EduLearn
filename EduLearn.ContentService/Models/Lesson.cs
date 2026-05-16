using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.ContentService.Models
{
    public class Lesson
    {
        [Key]
        public int LessonId { get; set; }

        [Required]
        public int SectionId { get; set; }

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        public string? ContentUrl { get; set; } // Azure Blob SAS URL or external link

        [Required]
        public ContentType ContentType { get; set; }

        public int? Duration { get; set; } // In seconds for video, minutes for reading

        public int DisplayOrder { get; set; }

        public bool IsPreview { get; set; } // If true, guest users can see this lesson

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for section
        [ForeignKey("SectionId")]
        public Section? Section { get; set; }
    }
}
