namespace EduLearn.ContentService.DTOs
{
    public class SectionResponseDto
    {
        public int SectionId { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<LessonResponseDto> Lessons { get; set; } = new();
    }
}
