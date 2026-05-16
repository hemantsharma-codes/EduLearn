using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EduLearn.CourseService.DTOs;
using EduLearn.CourseService.Models;
using EduLearn.SharedLib.Exceptions;
using EduLearn.CourseService.Repositories;
using EduLearn.SharedLib.Services;
using Microsoft.AspNetCore.Http;
using MassTransit;
using EduLearn.SharedLib.Messaging;

namespace EduLearn.CourseService.Services
{
    // implementation of the Course Service managing business logic for the EduLearn Course Catalog
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IMapper _mapper;
        private readonly IAzureStorageService _storageService;
        private readonly IPublishEndpoint _publishEndpoint;

        public CourseService(ICourseRepository courseRepository, IMapper mapper, IAzureStorageService storageService, IPublishEndpoint publishEndpoint)
        {
            _courseRepository = courseRepository;
            _mapper = mapper;
            _storageService = storageService;
            _publishEndpoint = publishEndpoint;
        }

        // <inheritdoc />
        public async Task<CourseResponseDto> CreateCourseAsync(int instructorId, string instructorName, CreateCourseRequestDto dto, IFormFile? thumbnail)
        {
            var course = _mapper.Map<Course>(dto);
            course.InstructorId = instructorId;
            course.InstructorName = instructorName;
            course.IsPublished = false; 
            course.IsApproved = false;  
            course.CreatedAt = DateTime.UtcNow;

            if (thumbnail != null && thumbnail.Length > 0)
            {
                course.ThumbnailUrl = await _storageService.UploadAsync(thumbnail, "thumbnails");
            }

            // Ensure we always have a thumbnail
            if (string.IsNullOrEmpty(course.ThumbnailUrl))
            {
                course.ThumbnailUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=400&q=80";
            }

            await _courseRepository.AddAsync(course);
            await _courseRepository.SaveChangesAsync();

            var response = _mapper.Map<CourseResponseDto>(course);
            if (!string.IsNullOrEmpty(response.ThumbnailUrl))
            {
                response.ThumbnailUrl = _storageService.GenerateSasUrl(response.ThumbnailUrl);
            }

            // Notify other services (like ContentService) that a valid course has been created
            await _publishEndpoint.Publish<ICourseCreatedEvent>(new 
            { 
                CourseId = course.CourseId, 
                Title = course.Title,
                InstructorId = course.InstructorId,
                ThumbnailUrl = course.ThumbnailUrl
            });

            return response;
        }

        // <inheritdoc />
        public async Task<CourseResponseDto?> GetCourseByIdAsync(int courseId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                throw new NotFoundException("The requested course could not be found.");
            }
            
            var dto = _mapper.Map<CourseResponseDto>(course);
            if (!string.IsNullOrEmpty(dto.ThumbnailUrl))
            {
                dto.ThumbnailUrl = _storageService.GenerateSasUrl(dto.ThumbnailUrl);
            }
            return dto;
        }

        // <inheritdoc />
        public async Task<IEnumerable<CourseResponseDto>> GetCoursesByInstructorAsync(int instructorId)
        {
            // Instructors can see all their own courses (Draft, Published, Approved)
            var courses = await _courseRepository.FindByInstructorIdAsync(instructorId);
            var dtos = _mapper.Map<IEnumerable<CourseResponseDto>>(courses);
            SignThumbnailUrls(dtos);
            return dtos;
        }

        // <inheritdoc />
        public async Task<IEnumerable<CourseResponseDto>> GetCoursesByCategoryAsync(string category)
        {
            var courses = await _courseRepository.FindByCategoryAsync(category);
            // Public users should only see courses that are both Published and Approved
            var approvedCourses = courses.Where(c => c.IsPublished && c.IsApproved);
            var dtos = _mapper.Map<IEnumerable<CourseResponseDto>>(approvedCourses);
            SignThumbnailUrls(dtos);
            return dtos;
        }

        // <inheritdoc />
        public async Task<IEnumerable<CourseResponseDto>> GetPublishedCoursesAsync()
        {
            var courses = await _courseRepository.FindByIsPublishedAsync(true);
            var approvedCourses = courses.Where(c => c.IsApproved);
            var dtos = _mapper.Map<IEnumerable<CourseResponseDto>>(approvedCourses);
            SignThumbnailUrls(dtos);
            return dtos;
        }

        public async Task<IEnumerable<CourseResponseDto>> GetAllCoursesAsync()
        {
            var courses = await _courseRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<CourseResponseDto>>(courses);
            SignThumbnailUrls(dtos);
            return dtos;
        }

        // <inheritdoc />
        public async Task<IEnumerable<CourseResponseDto>> SearchCoursesAsync(string keyword)
        {
            var courses = await _courseRepository.SearchCoursesAsync(keyword);
            // Filtering at service level to ensure consistency in visibility rules
            var approvedCourses = courses.Where(c => c.IsPublished && c.IsApproved);
            var dtos = _mapper.Map<IEnumerable<CourseResponseDto>>(approvedCourses);
            SignThumbnailUrls(dtos);
            return dtos;
        }

        // <inheritdoc />
        public async Task<bool> UpdateCourseAsync(int courseId, int instructorId, UpdateCourseRequestDto dto)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                throw new NotFoundException("Course not found.");
            }

            // Security Check: Only the owning instructor can update their course
            if (course.InstructorId != instructorId)
            {
                throw new UnauthorizedException("You do not have permission to update this course.");
            }

            course.Title = dto.Title;
            course.Description = dto.Description;
            course.Category = dto.Category;
            course.Level = dto.Level;
            course.Language = dto.Language;
            course.Price = dto.Price;
            course.ThumbnailUrl = dto.ThumbnailUrl;
            course.UpdatedAt = DateTime.UtcNow;
            
            // Hardening: Any major update resets the approval status to ensure content review
            course.IsPublished = false;
            course.IsApproved = false;

            await _courseRepository.UpdateAsync(course);
            await _courseRepository.SaveChangesAsync();

            return true;
        }

        // <inheritdoc />
        public async Task<bool> PublishCourseAsync(int courseId, int instructorId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                throw new NotFoundException("Course not found.");
            }

            if (course.InstructorId != instructorId)
            {
                throw new UnauthorizedException("You do not have permission to publish this course.");
            }

            course.IsPublished = true;
            await _courseRepository.UpdateAsync(course);
            await _courseRepository.SaveChangesAsync();

            return true;
        }

        // <inheritdoc />
        public async Task<bool> ApproveCourseAsync(int courseId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                throw new NotFoundException("Course not found.");
            }

            course.IsApproved = true;
            await _courseRepository.UpdateAsync(course);
            await _courseRepository.SaveChangesAsync();

            return true;
        }

        // <inheritdoc />
        public async Task<bool> DeleteCourseAsync(int courseId, int instructorId)
        {
            var course = await _courseRepository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                throw new NotFoundException("Course not found.");
            }
            
            // Access Control: InstructorId == 0 bypasses check for Admins
            if (instructorId != 0 && course.InstructorId != instructorId)
            {
                throw new UnauthorizedException("You do not have permission to delete this course.");
            }

            await _courseRepository.DeleteAsync(course);
            await _courseRepository.SaveChangesAsync();

            // Publish Event to notify ContentService and other services
            await _publishEndpoint.Publish<ICourseDeletedEvent>(new { CourseId = courseId });

            return true;
        }

        // <inheritdoc />
        public async Task<IEnumerable<CourseResponseDto>> GetTopRatedCoursesAsync(int count)
        {
            var courses = await _courseRepository.FindTopRatedAsync(count);
            var dtos = _mapper.Map<IEnumerable<CourseResponseDto>>(courses);
            SignThumbnailUrls(dtos);
            return dtos;
        }

        // <inheritdoc />
        public async Task IncrementEnrollmentAsync(int courseId)
        {
            await _courseRepository.IncrementEnrollmentAsync(courseId);
        }

        public async Task DecrementEnrollmentAsync(int courseId)
        {
            await _courseRepository.DecrementEnrollmentAsync(courseId);
        }

        private void SignThumbnailUrls(IEnumerable<CourseResponseDto> dtos)
        {
            foreach (var dto in dtos)
            {
                if (!string.IsNullOrEmpty(dto.ThumbnailUrl))
                {
                    dto.ThumbnailUrl = _storageService.GenerateSasUrl(dto.ThumbnailUrl);
                }
            }
        }
    }
}

