using EduLearn.ContentService.DTOs;
using EduLearn.ContentService.Services;
using EduLearn.SharedLib.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EduLearn.ContentService.Controllers
{
    // manages the course curriculum including sections and lessons
    // supports media uploads and secure content delivery via SAS URLs
    [ApiController]
    [Route("api/content")]
    public class ContentController : ControllerBase
    {
        private readonly IContentService _contentService;

        public ContentController(IContentService contentService)
        {
            _contentService = contentService;
        }

        // --- Sections ---

        // get all sections for a specific course, including their lessons
        [HttpGet("courses/{courseId}/sections")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<SectionResponseDto>>), 200)]
        public async Task<IActionResult> GetSectionsByCourse(int courseId)
        {
            var sections = await _contentService.GetSectionsByCourseAsync(courseId);
            return Ok(ApiResponse<IEnumerable<SectionResponseDto>>.SuccessResult(sections));
        }

        // creates a new section within a course
        [Authorize(Roles = "INSTRUCTOR")]
        [HttpPost("sections")]
        [ProducesResponseType(typeof(ApiResponse<SectionResponseDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CreateSection([FromBody] CreateSectionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _contentService.CreateSectionAsync(dto);
            return Ok(ApiResponse<SectionResponseDto>.SuccessResult(result, "Section created successfully."));
        }

        // update an existing section's details
        [Authorize(Roles = "INSTRUCTOR")]
        [HttpPut("sections/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateSection(int id, [FromBody] CreateSectionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _contentService.UpdateSectionAsync(id, dto);
            return Ok(ApiResponse<object>.SuccessResult(null, "Section updated successfully."));
        }

        // deletes a section and all its associated lessons and media files
        [Authorize(Roles = "INSTRUCTOR,ADMIN")]
        [HttpDelete("sections/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteSection(int id)
        {
            await _contentService.DeleteSectionAsync(id);
            return Ok(ApiResponse<object>.SuccessResult(null, "Section deleted successfully."));
        }

        // --- Lessons ---

        // get all lessons for a specific section
        [HttpGet("sections/{sectionId}/lessons")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<LessonResponseDto>>), 200)]
        public async Task<IActionResult> GetLessonsBySection(int sectionId)
        {
            var lessons = await _contentService.GetLessonsBySectionAsync(sectionId);
            return Ok(ApiResponse<IEnumerable<LessonResponseDto>>.SuccessResult(lessons));
        }

        // get preview lessons for a course. Publicly accessible but rate-limited
        [EnableRateLimiting("PreviewPolicy")]
        [HttpGet("courses/{courseId}/preview")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<LessonResponseDto>>), 200)]
        public async Task<IActionResult> GetPreviewLessons(int courseId)
        {
            var lessons = await _contentService.GetPreviewLessonsByCourseAsync(courseId);
            return Ok(ApiResponse<IEnumerable<LessonResponseDto>>.SuccessResult(lessons));
        }

        // get full details of a lesson, including a secure SAS URL for its content
        [HttpGet("lessons/{id}")]
        [ProducesResponseType(typeof(ApiResponse<LessonResponseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetLessonById(int id)
        {
            var lesson = await _contentService.GetLessonByIdAsync(id);
            if (lesson == null) return NotFound(ApiResponse<object>.FailureResult("Lesson not found."));
            
            return Ok(ApiResponse<LessonResponseDto>.SuccessResult(lesson));
        }

        // creates a new lesson with either a file upload (Azure) or an external URL
        [Authorize(Roles = "INSTRUCTOR")]
        [HttpPost("lessons")]
        [ProducesResponseType(typeof(ApiResponse<LessonResponseDto>), 200)]
        public async Task<IActionResult> CreateLesson([FromForm] CreateLessonDto dto, IFormFile? file)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            if ((file == null || file.Length == 0) && string.IsNullOrEmpty(dto.ContentUrl))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Either a lesson file or an external content URL is required."));
            }

            var result = await _contentService.CreateLessonAsync(dto, file);
            return Ok(ApiResponse<LessonResponseDto>.SuccessResult(result, "Lesson created successfully."));
        }

        // update an existing lesson's metadata
        [Authorize(Roles = "INSTRUCTOR")]
        [HttpPut("lessons/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpdateLesson(int id, [FromBody] CreateLessonDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _contentService.UpdateLessonAsync(id, dto);
            return Ok(ApiResponse<object>.SuccessResult(null, "Lesson updated successfully."));
        }

        // deletes a lesson and its associated media from Azure storage
        [Authorize(Roles = "INSTRUCTOR,ADMIN")]
        [HttpDelete("lessons/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            await _contentService.DeleteLessonAsync(id);
            return Ok(ApiResponse<object>.SuccessResult(null, "Lesson deleted successfully."));
        }

        // reorders lessons within a course based on a list of IDs
        [Authorize(Roles = "INSTRUCTOR")]
        [HttpPut("courses/{courseId}/reorder-lessons")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ReorderLessons(int courseId, [FromBody] IList<int> lessonIds)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _contentService.ReorderLessonsAsync(courseId, lessonIds);
            return Ok(ApiResponse<object>.SuccessResult(null, "Lessons reordered successfully."));
        }
    }
}

