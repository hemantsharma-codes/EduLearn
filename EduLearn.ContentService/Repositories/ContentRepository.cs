using EduLearn.ContentService.Data;
using EduLearn.ContentService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ContentService.Repositories
{
    public class ContentRepository : IContentRepository
    {
        private readonly ContentDbContext _context;

        public ContentRepository(ContentDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsCourseValidAsync(int courseId)
        {
            return await _context.ValidCourses.AsNoTracking().AnyAsync(c => c.CourseId == courseId);
        }

        public async Task<IEnumerable<Section>> GetSectionsByCourseAsync(int courseId)
        {
            return await _context.Sections
                .AsNoTracking()
                .Include(s => s.Lessons)
                .Where(s => s.CourseId == courseId)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Section?> GetSectionByIdAsync(int sectionId)
        {
            return await _context.Sections.AsNoTracking().Include(s => s.Lessons).FirstOrDefaultAsync(s => s.SectionId == sectionId);
        }

        public async Task<Section> AddSectionAsync(Section section)
        {
            _context.Sections.Add(section);
            await _context.SaveChangesAsync();
            return section;
        }

        public async Task UpdateSectionAsync(Section section)
        {
            _context.Entry(section).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSectionAsync(int sectionId)
        {
            var section = await _context.Sections.FindAsync(sectionId);
            if (section != null)
            {
                _context.Sections.Remove(section);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Lesson>> GetLessonsBySectionAsync(int sectionId)
        {
            return await _context.Lessons
                .AsNoTracking()
                .Where(l => l.SectionId == sectionId)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lesson>> GetPreviewLessonsByCourseAsync(int courseId)
        {
            return await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Section)
                .Where(l => l.Section!.CourseId == courseId && l.IsPreview)
                .OrderBy(l => l.Section!.DisplayOrder)
                .ThenBy(l => l.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Lesson?> GetLessonByIdAsync(int lessonId)
        {
            return await _context.Lessons.FindAsync(lessonId);
        }

        public async Task<Lesson> AddLessonAsync(Lesson lesson)
        {
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
            return lesson;
        }

        public async Task UpdateLessonAsync(Lesson lesson)
        {
            _context.Entry(lesson).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteLessonAsync(int lessonId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson != null)
            {
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReorderLessonsAsync(int courseId, IList<int> lessonIds)
        {
            // Implementation of Section 2.3 requirements: batch update
            for (int i = 0; i < lessonIds.Count; i++)
            {
                var id = lessonIds[i];
                await _context.Lessons
                    .Where(l => l.LessonId == id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.DisplayOrder, i + 1));
            }
        }

        public async Task<int> GetTotalLessonsAsync(int courseId)
        {
            return await _context.Lessons.CountAsync(l => l.Section!.CourseId == courseId);
        }

        public async Task<int> GetCourseIdBySectionIdAsync(int sectionId)
        {
            var section = await _context.Sections.AsNoTracking().FirstOrDefaultAsync(s => s.SectionId == sectionId);
            return section?.CourseId ?? 0;
        }
    }
}
