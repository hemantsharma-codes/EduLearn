using AutoMapper;
using EduLearn.ContentService.DTOs;
using EduLearn.ContentService.Models;
using EduLearn.ContentService.Repositories;
using EduLearn.SharedLib.Services;
using EduLearn.SharedLib.Messaging;
using MassTransit;

namespace EduLearn.ContentService.Services
{
    public class ContentService : IContentService
    {
        private readonly IContentRepository _repository;
        private readonly IAzureStorageService _storageService;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;

        public ContentService(IContentRepository repository, IAzureStorageService storageService, IMapper mapper, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _storageService = storageService;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<IEnumerable<SectionResponseDto>> GetSectionsByCourseAsync(int courseId)
        {
            var sections = await _repository.GetSectionsByCourseAsync(courseId);
            var dtos = _mapper.Map<IEnumerable<SectionResponseDto>>(sections);

            foreach (var section in dtos)
            {
                foreach (var lesson in section.Lessons)
                {
                    if (!string.IsNullOrEmpty(lesson.ContentUrl))
                    {
                        lesson.ContentUrl = _storageService.GenerateSasUrl(lesson.ContentUrl);
                    }
                }
            }
            return dtos;
        }

        public async Task<SectionResponseDto> CreateSectionAsync(CreateSectionDto dto)
        {
            if (!await _repository.IsCourseValidAsync(dto.CourseId))
            {
                throw new EduLearn.SharedLib.Exceptions.NotFoundException($"Course with ID {dto.CourseId} is not valid or does not exist.");
            }

            var section = _mapper.Map<Section>(dto);
            await _repository.AddSectionAsync(section);
            return _mapper.Map<SectionResponseDto>(section);
        }

        public async Task UpdateSectionAsync(int id, CreateSectionDto dto)
        {
            var section = await _repository.GetSectionByIdAsync(id);
            if (section == null) throw new KeyNotFoundException("Section not found.");

            _mapper.Map(dto, section);
            await _repository.UpdateSectionAsync(section);
        }

        public async Task DeleteSectionAsync(int id)
        {
            var section = await _repository.GetSectionByIdAsync(id);
            if (section != null)
            {
                int courseId = section.CourseId;
                // Delete all lesson blobs in this section
                foreach (var lesson in section.Lessons)
                {
                    if (!string.IsNullOrEmpty(lesson.ContentUrl))
                    {
                        await _storageService.DeleteAsync(lesson.ContentUrl);
                    }
                }
                
                await _repository.DeleteSectionAsync(id);
                await PublishLessonCountUpdatedAsync(courseId);
            }
        }

        public async Task<IEnumerable<LessonResponseDto>> GetLessonsBySectionAsync(int sectionId)
        {
            var lessons = await _repository.GetLessonsBySectionAsync(sectionId);
            var dtos = _mapper.Map<IEnumerable<LessonResponseDto>>(lessons);

            foreach (var dto in dtos)
            {
                if (!string.IsNullOrEmpty(dto.ContentUrl))
                {
                    dto.ContentUrl = _storageService.GenerateSasUrl(dto.ContentUrl);
                }
            }
            return dtos;
        }

        public async Task<IEnumerable<LessonResponseDto>> GetPreviewLessonsByCourseAsync(int courseId)
        {
            var lessons = await _repository.GetPreviewLessonsByCourseAsync(courseId);
            var dtos = _mapper.Map<IEnumerable<LessonResponseDto>>(lessons);

            foreach (var dto in dtos)
            {
                if (!string.IsNullOrEmpty(dto.ContentUrl))
                {
                    dto.ContentUrl = _storageService.GenerateSasUrl(dto.ContentUrl);
                }
            }
            return dtos;
        }

        public async Task<LessonResponseDto?> GetLessonByIdAsync(int id)
        {
            var lesson = await _repository.GetLessonByIdAsync(id);
            if (lesson == null) return null;

            var dto = _mapper.Map<LessonResponseDto>(lesson);
            if (!string.IsNullOrEmpty(dto.ContentUrl))
            {
                dto.ContentUrl = _storageService.GenerateSasUrl(dto.ContentUrl);
            }
            return dto;
        }

        public async Task<LessonResponseDto> CreateLessonAsync(CreateLessonDto dto, IFormFile? file)
        {
            var lesson = _mapper.Map<Lesson>(dto);
            
            if (file != null && file.Length > 0)
            {
                // 1. Determine container (videos or documents)
                string container = dto.ContentType.ToUpper() == "VIDEO" ? "videos" : "documents";

                // 2. Upload to Azure Storage via SharedLib Service
                lesson.ContentUrl = await _storageService.UploadAsync(file, container);
            }
            else
            {
                // Use the provided external URL (YouTube, External Link, etc.)
                lesson.ContentUrl = dto.ContentUrl;
            }

            await _repository.AddLessonAsync(lesson);

            int courseId = await _repository.GetCourseIdBySectionIdAsync(lesson.SectionId);
            if (courseId > 0)
            {
                await PublishLessonCountUpdatedAsync(courseId);
            }

            return _mapper.Map<LessonResponseDto>(lesson);
        }

        public async Task UpdateLessonAsync(int id, CreateLessonDto dto)
        {
            var lesson = await _repository.GetLessonByIdAsync(id);
            if (lesson == null) throw new KeyNotFoundException("Lesson not found.");

            _mapper.Map(dto, lesson);
            await _repository.UpdateLessonAsync(lesson);
        }

        public async Task DeleteLessonAsync(int id)
        {
            var lesson = await _repository.GetLessonByIdAsync(id);
            if (lesson != null)
            {
                int courseId = await _repository.GetCourseIdBySectionIdAsync(lesson.SectionId);
                
                if (!string.IsNullOrEmpty(lesson.ContentUrl))
                {
                    // Delete the file from Azure Storage
                    await _storageService.DeleteAsync(lesson.ContentUrl);
                }

                await _repository.DeleteLessonAsync(id);
                
                if (courseId > 0)
                {
                    await PublishLessonCountUpdatedAsync(courseId);
                }
            }
        }

        public async Task ReorderLessonsAsync(int courseId, IList<int> lessonIds)
        {
            await _repository.ReorderLessonsAsync(courseId, lessonIds);
        }

        private async Task PublishLessonCountUpdatedAsync(int courseId)
        {
            int totalLessons = await _repository.GetTotalLessonsAsync(courseId);
            await _publishEndpoint.Publish<ILessonCountUpdatedEvent>(new
            {
                CourseId = courseId,
                TotalLessons = totalLessons
            });
        }
    }
}
