using EduLearn.EnrollmentService.DTOs;
using EduLearn.EnrollmentService.Services;
using EduLearn.SharedLib.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.EnrollmentService.Controllers
{
    [ApiController]
    [Route("api/enrollments")]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        // enroll a student in a course. (Student only)
        [Authorize(Roles = "STUDENT")]
        [HttpPost]
        public async Task<IActionResult> Enroll([FromBody] EnrollmentRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var studentId = GetCurrentUserId();
            if (studentId == 0)
                return Unauthorized(ApiResponse<object>.FailureResult("Invalid identity claims."));

            var studentEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? "";
            var studentName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? "Student";

            var result = await _enrollmentService.EnrollAsync(studentId, studentEmail, studentName, dto);
            return CreatedAtAction(nameof(GetEnrollmentById), new { id = result.EnrollmentId },
                ApiResponse<EnrollmentResponseDto>.SuccessResult(result, "Enrolled successfully."));
        }

        // get enrollment details by ID. (Owner, Instructor, Admin only)
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEnrollmentById(int id)
        {
            var result = await _enrollmentService.GetEnrollmentByIdAsync(id);
            if (result == null)
                return NotFound(ApiResponse<object>.FailureResult("Enrollment not found."));

            var userId = GetCurrentUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);   // FIXED: use ClaimTypes.Role

            // Allow only: Admin, Instructor, or the student who owns this enrollment
            if (role != "ADMIN" && role != "INSTRUCTOR" && result.StudentId != userId)
                return Forbid();   // 403 Forbidden

            return Ok(ApiResponse<EnrollmentResponseDto>.SuccessResult(result));
        }

        // get all enrollments for the currently authenticated student
        [Authorize(Roles = "STUDENT")]
        [HttpGet("my-enrollments")]
        public async Task<IActionResult> GetMyEnrollments()
        {
            var studentId = GetCurrentUserId();
            if (studentId == 0)
                return Unauthorized(ApiResponse<object>.FailureResult("Invalid identity claims."));

            var results = await _enrollmentService.GetEnrollmentsByStudentAsync(studentId);
            return Ok(ApiResponse<IEnumerable<EnrollmentResponseDto>>.SuccessResult(results));
        }

        // get enrollments for a specific student ID
        // - Students can only see their own
        // - Admins and Instructors can see any student's enrollments
        [Authorize]  // No role restriction, manual check inside
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetEnrollmentsByStudent(int studentId)
        {
            var currentUserId = GetCurrentUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role != "ADMIN" && role != "INSTRUCTOR" && currentUserId != studentId)
                return Forbid();

            var results = await _enrollmentService.GetEnrollmentsByStudentAsync(studentId);
            return Ok(ApiResponse<IEnumerable<EnrollmentResponseDto>>.SuccessResult(results));
        }

        // get all enrollments for a specific course. (Instructor or Admin only)
        [Authorize(Roles = "INSTRUCTOR,ADMIN")]
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetEnrollmentsByCourse(int courseId)
        {
            var results = await _enrollmentService.GetEnrollmentsByCourseAsync(courseId);
            return Ok(ApiResponse<IEnumerable<EnrollmentResponseDto>>.SuccessResult(results));
        }

        // check if the current student is enrolled in a course
        [Authorize]  // Any authenticated user can check their own enrollment status
        [HttpGet("is-enrolled/{courseId}")]
        public async Task<IActionResult> CheckEnrollment(int courseId)
        {
            var studentId = GetCurrentUserId();
            if (studentId == 0)
                return Unauthorized(ApiResponse<object>.FailureResult("Invalid identity claims."));

            var isEnrolled = await _enrollmentService.IsEnrolledAsync(studentId, courseId);
            return Ok(ApiResponse<bool>.SuccessResult(isEnrolled));
        }

        // drop (unenroll) from a course. (Student only)
        [Authorize(Roles = "STUDENT")]
        [HttpPut("course/{courseId}/drop")]
        public async Task<IActionResult> DropCourse(int courseId)
        {
            var studentId = GetCurrentUserId();
            if (studentId == 0)
                return Unauthorized(ApiResponse<object>.FailureResult("Invalid identity claims."));

            await _enrollmentService.DropCourseAsync(studentId, courseId);
            return Ok(ApiResponse<object>.SuccessResult(null, "Successfully dropped the course."));
        }

        // get total enrollment count for a course. (Public)
        [HttpGet("course/{courseId}/count")]
        [AllowAnonymous]  // If you want public, else remove this line and add [Authorize]
        public async Task<IActionResult> GetEnrollmentCount(int courseId)
        {
            var count = await _enrollmentService.GetEnrollmentCountAsync(courseId);
            return Ok(ApiResponse<int>.SuccessResult(count));
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
