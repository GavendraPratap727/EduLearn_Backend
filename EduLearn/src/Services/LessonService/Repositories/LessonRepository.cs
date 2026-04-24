using EduLearn.LessonService.Data;
using EduLearn.LessonService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.LessonService.Repositories
{
    public class LessonRepository : ILessonRepository
    {
        private readonly LessonDbContext _context;

        public LessonRepository(LessonDbContext context)
        {
            _context = context;
        }

        public async Task<Lesson?> FindByLessonIdAsync(Guid lessonId)
        {
            return await _context.Lessons.FirstOrDefaultAsync(l => l.LessonId == lessonId);
        }

        public async Task<List<Lesson>> FindByCourseIdAsync(Guid courseId)
        {
            return await _context.Lessons.Where(l => l.CourseId == courseId).ToListAsync();
        }

        public async Task<List<Lesson>> FindByCourseIdOrderByDisplayOrderAsync(Guid courseId)
        {
            return await _context.Lessons
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<Lesson>> FindByContentTypeAsync(ContentType contentType)
        {
            return await _context.Lessons.Where(l => l.ContentType == contentType).ToListAsync();
        }

        public async Task<List<Lesson>> FindPreviewLessonsAsync(Guid courseId)
        {
            return await _context.Lessons
                .Where(l => l.CourseId == courseId && l.IsPreview)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();
        }

        public async Task<int> CountByCourseIdAsync(Guid courseId)
        {
            return await _context.Lessons.CountAsync(l => l.CourseId == courseId);
        }

        public async Task<Lesson> AddAsync(Lesson lesson)
        {
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
            return lesson;
        }

        public async Task<Lesson> UpdateAsync(Lesson lesson)
        {
            _context.Lessons.Update(lesson);
            await _context.SaveChangesAsync();
            return lesson;
        }

        public async Task DeleteAsync(Guid lessonId)
        {
            var lesson = await FindByLessonIdAsync(lessonId);
            if (lesson != null)
            {
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByCourseIdAsync(Guid courseId)
        {
            var lessons = await FindByCourseIdAsync(courseId);
            _context.Lessons.RemoveRange(lessons);
            await _context.SaveChangesAsync();
        }

        public async Task ReorderLessonsAsync(List<(Guid lessonId, int displayOrder)> lessonOrders)
        {
            foreach (var (lessonId, displayOrder) in lessonOrders)
            {
                await _context.Lessons
                    .Where(l => l.LessonId == lessonId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.DisplayOrder, displayOrder));
            }
        }
    }
}
