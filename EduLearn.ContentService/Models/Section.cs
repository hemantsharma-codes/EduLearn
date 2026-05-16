using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.ContentService.Models
{
    public class Section
    {
        [Key]
        public int SectionId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for lessons
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
