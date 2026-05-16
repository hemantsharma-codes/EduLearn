using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.ContentService.Models
{
    // local replica of valid courses from CourseService
    // maintained via Event-Driven Architecture (RabbitMQ)
    public class ValidCourse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // We keep the exact CourseId from CourseService
        public int CourseId { get; set; }
        
        public string Title { get; set; } = string.Empty;
    }
}

