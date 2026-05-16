namespace EduLearn.ContentService.DTOs
{
    public class LessonResponseDto
    {
        public int LessonId { get; set; }
        public int SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ContentUrl { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPreview { get; set; }
    }
}
