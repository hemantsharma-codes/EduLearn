using System.ComponentModel.DataAnnotations;

namespace EduLearn.CourseService.DTOs
{
    public class CreateCourseRequestDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [MinLength(20, ErrorMessage = "Description must be at least 20 characters long")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Level is required")]
        [MaxLength(50, ErrorMessage = "Level cannot exceed 50 characters")]
        public string Level { get; set; } = string.Empty;

        [Required(ErrorMessage = "Language is required")]
        [MaxLength(50, ErrorMessage = "Language cannot exceed 50 characters")]
        public string Language { get; set; } = string.Empty;

        [Range(0, 10000, ErrorMessage = "Price must be between 0 and 10000")]
        public decimal Price { get; set; }

        [Url(ErrorMessage = "ThumbnailUrl must be a valid URL")]
        public string? ThumbnailUrl { get; set; }
    }
}
