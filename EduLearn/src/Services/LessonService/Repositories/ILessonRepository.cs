using EduLearn.LessonService.Models;

namespace EduLearn.LessonService.Repositories
{
    public interface ILessonRepository
    {
        Task<Lesson?> FindByLessonIdAsync(Guid lessonId);
        Task<List<Lesson>> FindByCourseIdAsync(Guid courseId);
        Task<List<Lesson>> FindByCourseIdOrderByDisplayOrderAsync(Guid courseId);
        Task<List<Lesson>> FindByContentTypeAsync(ContentType contentType);
        Task<List<Lesson>> FindPreviewLessonsAsync(Guid courseId);
        Task<int> CountByCourseIdAsync(Guid courseId);
        Task<Lesson> AddAsync(Lesson lesson);
        Task<Lesson> UpdateAsync(Lesson lesson);
        Task DeleteAsync(Guid lessonId);
        Task DeleteByCourseIdAsync(Guid courseId);
        Task ReorderLessonsAsync(List<(Guid lessonId, int displayOrder)> lessonOrders);
    }
}
