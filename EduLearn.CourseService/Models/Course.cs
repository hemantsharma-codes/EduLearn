using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.CourseService.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }
        
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public int InstructorId { get; set; }
        public string InstructorName { get; set; } = "Expert Instructor";
        
        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [Required, MaxLength(50)]
        public string Level { get; set; } = string.Empty;
        
        [Required, MaxLength(50)]
        public string Language { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public string? ThumbnailUrl { get; set; }
        
        public bool IsPublished { get; set; }
        public bool IsApproved { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public int TotalDuration { get; set; } // In minutes
        public int EnrollmentCount { get; set; }
    }
}
