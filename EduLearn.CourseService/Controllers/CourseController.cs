using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EduLearn.CourseService.DTOs;
using EduLearn.CourseService.Services;
using EduLearn.SharedLib.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLearn.CourseService.Controllers
{
    // controller for managing the course catalog, including instructor and admin operations
    [ApiController]
    [Route("api/courses")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        // creates a new course draft. (Requires INSTRUCTOR role)
        [Authorize(Roles = "INSTRUCTOR")]
        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromForm] CreateCourseRequestDto dto, IFormFile? thumbnail)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var instructorId = GetCurrentUserId();
            var instructorName = User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.Name) ?? "Expert Instructor";
            if (instructorId == 0) return Unauthorized(ApiResponse<object>.FailureResult("Invalid identity claims."));

            var course = await _courseService.CreateCourseAsync(instructorId, instructorName, dto, thumbnail);
            return CreatedAtAction(nameof(GetCourseById), new { id = course.CourseId }, ApiResponse<CourseResponseDto>.SuccessResult(course, "Course draft created successfully."));
        }

        // get details of a specific course. Public if approved, private if draft
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseById(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);

            // Hardening: Prevent public access to drafts or unapproved courses
            if (course != null && (!course.IsPublished || !course.IsApproved))
            {
                if (!User.Identity?.IsAuthenticated ?? false)
                {
                    return Unauthorized(ApiResponse<object>.FailureResult("Authentication required to view non-public courses."));
                }

                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("ADMIN");

                // Only Admin or the Instructor who created it can see non-public courses
                if (!isAdmin && course.InstructorId != userId)
                {
                    return StatusCode(403, ApiResponse<object>.FailureResult("Access denied to this course draft."));
                }
            }

            return Ok(ApiResponse<CourseResponseDto>.SuccessResult(course!));
        }

        // get all courses for the currently logged-in instructor
        [Authorize(Roles = "INSTRUCTOR")]
        [HttpGet("instructor/my-courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            var instructorId = GetCurrentUserId();
            if (instructorId == 0) return Unauthorized(ApiResponse<object>.FailureResult("Invalid identity claims."));

            var courses = await _courseService.GetCoursesByInstructorAsync(instructorId);
            return Ok(ApiResponse<IEnumerable<CourseResponseDto>>.SuccessResult(courses));
        }

        // get all courses for administrative management. (Requires ADMIN role)
        [Authorize(Roles = "ADMIN")]
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllCoursesForAdmin()
        {
            var courses = await _courseService.GetAllCoursesAsync();
            return Ok(ApiResponse<IEnumerable<CourseResponseDto>>.SuccessResult(courses));
        }

        // get approved courses within a specific category
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetCoursesByCategory(string category)
        {
            var courses = await _courseService.GetCoursesByCategoryAsync(category);
            return Ok(ApiResponse<IEnumerable<CourseResponseDto>>.SuccessResult(courses));
        }

        // get all courses that are ready for public consumption
        [HttpGet]
        public async Task<IActionResult> GetPublishedCourses()
        {
            var courses = await _courseService.GetPublishedCoursesAsync();
            return Ok(ApiResponse<IEnumerable<CourseResponseDto>>.SuccessResult(courses));
        }

        // global search for approved courses
        [HttpGet("search")]
        public async Task<IActionResult> SearchCourses([FromQuery] string q)
        {
            var courses = await _courseService.SearchCoursesAsync(q ?? "");
            return Ok(ApiResponse<IEnumerable<CourseResponseDto>>.SuccessResult(courses));
        }

        // get the most popular courses based on enrollment
        [HttpGet("featured")]
        public async Task<IActionResult> GetTopRatedCourses([FromQuery] int limit = 10)
        {
            var courses = await _courseService.GetTopRatedCoursesAsync(limit);
            return Ok(ApiResponse<IEnumerable<CourseResponseDto>>.SuccessResult(courses));
        }

        // update a course. Resets approval status. (Requires INSTRUCTOR role)
        [Authorize(Roles = "INSTRUCTOR")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var instructorId = GetCurrentUserId();
            await _courseService.UpdateCourseAsync(id, instructorId, dto);

            return Ok(ApiResponse<object>.SuccessResult(null, "Course updated and submitted for re-approval."));
        }

        // submits a course for administrative review
        [Authorize(Roles = "INSTRUCTOR")]
        [HttpPatch("{id}/publish")]
        public async Task<IActionResult> PublishCourse(int id)
        {
            var instructorId = GetCurrentUserId();
            await _courseService.PublishCourseAsync(id, instructorId);

            return Ok(ApiResponse<object>.SuccessResult(null, "Course has been published and is pending admin approval."));
        }

        // approves a course for public visibility. (Requires ADMIN role)
        [Authorize(Roles = "ADMIN")]
        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> ApproveCourse(int id)
        {
            await _courseService.ApproveCourseAsync(id);
            return Ok(ApiResponse<object>.SuccessResult(null, "Course has been approved successfully."));
        }

        // deletes a course. (Admins or Owning Instructors only)
        [Authorize(Roles = "ADMIN,INSTRUCTOR")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("ADMIN");

            // Pass 0 if admin to bypass instructor ownership check
            int instructorIdToCheck = isAdmin ? 0 : userId;

            await _courseService.DeleteCourseAsync(id, instructorIdToCheck);

            return Ok(ApiResponse<object>.SuccessResult(null, "Course has been permanently removed."));
        }

        // internal endpoint to increment enrollment (typically called by an Order/Enrollment Service)
        [Authorize]
        [HttpPost("{id}/enroll")]
        public async Task<IActionResult> IncrementEnrollment(int id)
        {
            await _courseService.IncrementEnrollmentAsync(id);
            return Ok(ApiResponse<object>.SuccessResult(null, "Enrollment recorded."));
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? User.FindFirstValue("sub") 
                            ?? User.FindFirstValue("nameid");
            return int.TryParse(userIdString, out int userId) ? userId : 0;
        }
    }
}

