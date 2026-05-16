using AutoMapper;
using EduLearn.EnrollmentService.DTOs;
using EduLearn.EnrollmentService.Models;
using EduLearn.EnrollmentService.Repositories;
using EduLearn.SharedLib.Exceptions;
using EduLearn.SharedLib.Messaging;
using EduLearn.SharedLib.Services;
using MassTransit;

namespace EduLearn.EnrollmentService.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _repository;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IAzureStorageService _storageService;

        public EnrollmentService(
            IEnrollmentRepository repository, 
            IMapper mapper, 
            IPublishEndpoint publishEndpoint,
            IAzureStorageService storageService)
        {
            _repository = repository;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
            _storageService = storageService;
        }

        public async Task<EnrollmentResponseDto> EnrollAsync(int studentId, string studentEmail, string studentName, EnrollmentRequestDto requestDto)
        {
            var isEnrolled = await _repository.IsEnrolledAsync(studentId, requestDto.CourseId);
            if (isEnrolled)
            {
                throw new BadRequestException("Student is already enrolled in this course.");
            }

            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseId = requestDto.CourseId,
                PaymentId = requestDto.PaymentId,
                EnrolledAt = DateTime.UtcNow,
                Status = "ACTIVE"
            };

            await _repository.AddEnrollmentAsync(enrollment);

            // Fetch trusted title from local CourseRef
            var courseTitle = await _repository.GetCourseTitleAsync(requestDto.CourseId) ?? $"Course #{requestDto.CourseId}";

            // Publish event to RabbitMQ
            await _publishEndpoint.Publish<IEnrollmentCreatedEvent>(new
            {
                enrollment.EnrollmentId,
                enrollment.StudentId,
                enrollment.CourseId,
                CourseTitle = courseTitle,
                StudentEmail = studentEmail,
                StudentName = studentName,
                enrollment.EnrolledAt
            });

            var response = _mapper.Map<EnrollmentResponseDto>(enrollment);
            SignThumbnailUrl(response);
            return response;
        }

        public async Task<EnrollmentResponseDto> GetEnrollmentByIdAsync(int enrollmentId)
        {
            var enrollment = await _repository.FindByEnrollmentIdAsync(enrollmentId);
            if (enrollment == null) throw new NotFoundException("Enrollment not found.");

            var response = _mapper.Map<EnrollmentResponseDto>(enrollment);
            SignThumbnailUrl(response);
            return response;
        }

        public async Task<IEnumerable<EnrollmentResponseDto>> GetEnrollmentsByStudentAsync(int studentId)
        {
            var enrollments = await _repository.FindByStudentIdAsync(studentId);
            var response = _mapper.Map<IEnumerable<EnrollmentResponseDto>>(enrollments);
            foreach (var item in response) SignThumbnailUrl(item);
            return response;
        }

        private void SignThumbnailUrl(EnrollmentResponseDto dto)
        {
            if (!string.IsNullOrEmpty(dto.CourseThumbnail))
            {
                Console.WriteLine($"[DEBUG] Signing Thumbnail for Course {dto.CourseId}: {dto.CourseThumbnail}");
                var original = dto.CourseThumbnail;
                dto.CourseThumbnail = _storageService.GenerateSasUrl(dto.CourseThumbnail);
                
                if (original == dto.CourseThumbnail) {
                    Console.WriteLine($"[DEBUG] WARNING: Thumbnail URL was NOT modified by GenerateSasUrl!");
                } else {
                    Console.WriteLine($"[DEBUG] Successfully signed. New URL length: {dto.CourseThumbnail.Length}");
                }
            }
        }

        public async Task<IEnumerable<EnrollmentResponseDto>> GetEnrollmentsByCourseAsync(int courseId)
        {
            var enrollments = await _repository.FindByCourseIdAsync(courseId);
            return _mapper.Map<IEnumerable<EnrollmentResponseDto>>(enrollments);
        }

        public async Task<bool> IsEnrolledAsync(int studentId, int courseId)
        {
            return await _repository.IsEnrolledAsync(studentId, courseId);
        }

        public async Task DropCourseAsync(int studentId, int courseId)
        {
            var enrollment = await _repository.FindByStudentAndCourseAsync(studentId, courseId);
            if (enrollment == null)
            {
                throw new NotFoundException("Enrollment not found.");
            }

            if (enrollment.Status == "COMPLETED")
            {
                throw new BadRequestException("Cannot drop a completed course.");
            }

            enrollment.Status = "DROPPED";
            await _repository.UpdateEnrollmentAsync(enrollment);

            // Publish message to decrement the enrollment count
            await _publishEndpoint.Publish<IEnrollmentDroppedEvent>(new
            {
                StudentId = studentId,
                CourseId = courseId
            });
        }

        public async Task<int> GetEnrollmentCountAsync(int courseId)
        {
            return await _repository.CountByCourseIdAsync(courseId);
        }

        public async Task DeleteAllEnrollmentsByStudentAsync(int studentId)
        {
            var enrollments = await _repository.FindByStudentIdAsync(studentId);
            
            foreach (var enrollment in enrollments)
            {
                // We notify CourseService to decrement the count before we delete the record
                await _publishEndpoint.Publish<IEnrollmentDroppedEvent>(new
                {
                    StudentId = studentId,
                    CourseId = enrollment.CourseId
                });
            }

            await _repository.DeleteEnrollmentsByStudentAsync(studentId);
        }
    }
}
